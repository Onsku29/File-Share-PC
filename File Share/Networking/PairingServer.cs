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
        private readonly ServerInfoManager _serverInfoManager = new();
        public PairingInfo Info { get; }

        public PairingServer(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
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

        private record PairingRequest(string token, string deviceId, string deviceName);
        private record PairingResponse(string status, string? serverName = null, string? reason = null);

        private async Task HandlePairingAsync(StreamReader reader, StreamWriter writer, TcpClient client)
        {
            string? jsonRequest = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(jsonRequest))
            {
                await SendResponseAsync(writer, "failed", reason: "Empty request");
                Debug.WriteLine("Pairing failed: empty request");
                return;
            }

            PairingRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<PairingRequest>(jsonRequest);
            }
            catch (JsonException)
            {
                await SendResponseAsync(writer, "failed", reason: "Malformed JSON");
                Debug.WriteLine("Pairing failed: malformed JSON");
                return;
            }

            if (request == null
                || string.IsNullOrWhiteSpace(request.token)
                || string.IsNullOrWhiteSpace(request.deviceId)
                || string.IsNullOrWhiteSpace(request.deviceName))
            {
                await SendResponseAsync(writer, "failed", reason: "Missing fields");
                Debug.WriteLine("Pairing failed: missing fields");
                return;
            }

            if (request.token != Info.Token)
            {
                await SendResponseAsync(writer, "failed", reason: "Invalid token");
                Debug.WriteLine("Pairing failed: invalid token");
                return;
            }

            if (_deviceManager.IsDevicePaired(request.deviceId))
            {
                await SendResponseAsync(writer, "failed", reason: "Device already paired");
                Debug.WriteLine($"Pairing rejected: device already paired ({request.deviceId})");
                return;
            }

            // Save device
            string remoteIp = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
            var pairedDevice = new PairedDevice(request.deviceId, request.deviceName, remoteIp);
            _deviceManager.AddDevice(pairedDevice);

            await SendResponseAsync(writer, "success", serverName: _serverInfoManager.GetServerName());
            Debug.WriteLine($"Device paired successfully: {request.deviceName} ({request.deviceId})");
        }

        private static async Task SendResponseAsync(StreamWriter writer, string status, string? serverName = null, string? reason = null)
        {
            var response = new PairingResponse(status, serverName, reason);
            string jsonResponse = JsonSerializer.Serialize(response);
            await writer.WriteLineAsync(jsonResponse);
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