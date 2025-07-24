using File_Share;
using FileShare.Storing;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FileShare.Networking
{
    public class PairingInfo
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Token { get; set; }
        public string DeviceName { get; set; }
    }

    public class PairingServer
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly DeviceManager _deviceManager;
        private readonly AESKeyManager _aesKeyManager;
        private readonly ServerInfoManager _serverInfoManager = new();
        public PairingInfo Info { get; }

        public PairingServer(DeviceManager deviceManager, AESKeyManager aesKeyManager)
        {
            _deviceManager = deviceManager;
            _aesKeyManager = aesKeyManager;
            string ip = GetLocalIpAddress();
            int port = _serverInfoManager.GetServerPort();
            if (port == 0 || !IsPortAvailable(port))
            {
                port = FindAvailablePort();
            }

            string token = "";
            if(_serverInfoManager.GetServerToken() != string.Empty)
            {
                token = _serverInfoManager.GetServerToken();
            }
            else
            {
                token = Guid.NewGuid().ToString();
            }

            string deviceName = Environment.MachineName;

            Info = new PairingInfo
            {
                Ip = ip,
                Port = port,
                Token = token,
                DeviceName = deviceName
            };

            _serverInfoManager.SaveServerInfo(Info);

            _listener = new TcpListener(IPAddress.Any, port);
            _ = StartListeningAsync(_cts.Token);
        }

        private async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            try
            {
                _listener.Start();
                Debug.WriteLine($"Pairing server started on {Info.Ip}:{Info.Port}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    client.ReceiveBufferSize = 1024 * 1024;
                    client.SendBufferSize = 1024 * 1024;
                    _ = HandleClientAsync(client, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Pairing server error: {ex.Message}");
            }
        }

        private async Task HandlePairingAsync(StreamReader reader, StreamWriter writer, TcpClient client)
        {
            await writer.WriteLineAsync("SEND_TOKEN");
            string token = await reader.ReadLineAsync();
            Debug.WriteLine($"Received pairing token: {token}");

            if (token?.Trim() == Info.Token)
            {
                await writer.WriteLineAsync("SEND_ID");
                string deviceId = await reader.ReadLineAsync();

                if (_deviceManager.IsDevicePaired(deviceId))
                {
                    await writer.WriteLineAsync("ALREADY_PAIRED");
                    Debug.WriteLine("Pairing rejected: Device already paired.");
                    return;
                }

                await writer.WriteLineAsync("SEND_NAME");
                string deviceName = await reader.ReadLineAsync();

                if (!string.IsNullOrWhiteSpace(deviceId) && !string.IsNullOrWhiteSpace(deviceName))
                {
                    string remoteIp = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();

                    Debug.WriteLine($"Device paired: {deviceName} ({deviceId})");
                    await writer.WriteLineAsync("PAIRING_SUCCESS");
                    string response = await reader.ReadLineAsync();
                    if (response == "SEND_NAME")
                    {
                        await writer.WriteLineAsync(_serverInfoManager.GetServerName());
                        response = await reader.ReadLineAsync();
                        if (response == "PAIRING_SUCCESS")
                        {
                            PairedDevice pairedDevice = new PairedDevice(deviceId, deviceName, remoteIp);
                            _deviceManager.AddDevice(pairedDevice);
                            await writer.WriteLineAsync(_aesKeyManager.GetKeyForDevice(pairedDevice).Key);

                            Debug.WriteLine($"Device paired successfully: {deviceName} ({deviceId})");
                        }

                        else if (response == "PAIRING_FAILED")
                        {
                            Debug.WriteLine($"Pairing failed for device: {deviceName} ({deviceId})");
                        }

                        else
                        {
                            Debug.WriteLine($"Unexpected client response: {response}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Device name missing.");
                        await writer.WriteLineAsync("PAIRING_FAILED");
                    }
                }
                else
                {
                    await writer.WriteLineAsync("INVALID_TOKEN");
                    Debug.WriteLine("Invalid pairing attempt.");
                }
            }
        }


        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            try
            {
                string mode = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(mode))
                {
                    Debug.WriteLine("Client sent no initial mode.");
                    return;
                }
                if (mode == "PAIR")
                {
                    await HandlePairingAsync(reader, writer, client);
                }
                else if (mode == "SHARE")
                {
                    var fileHandler = new FileSharingHandler();
                    await fileHandler.HandleClientAsync(reader, writer, stream, async filename =>
                    {
                        var tcs = new TaskCompletionSource<string?>();

                        App.Instance.mainWindow.DispatcherQueue.TryEnqueue(async () =>
                        {
                            try
                            {
                                var path = await App.Instance.mainWindow.RequestSavePathAsync(filename);
                                tcs.SetResult(path);
                            }
                            catch (Exception ex)
                            {
                                tcs.SetException(ex);
                            }
                        });

                        return await tcs.Task;
                    });
                }
                else
                {
                    Debug.WriteLine($"Unknown mode received from client: {mode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Client handling error: {ex.Message}");
            }
        }

        private string GetLocalIpAddress()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        }

        private int FindAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private bool IsPortAvailable(int port)
        {
            try
            {
                TcpListener testListener = new TcpListener(IPAddress.Any, port);
                testListener.Start();
                testListener.Stop();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }


        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }
}