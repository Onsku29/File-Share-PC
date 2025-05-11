using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FileShare.Storing;

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
        private readonly DeviceManager _deviceManager = new();
        public event Action<string>? DevicePaired;
        public PairingInfo Info { get; }

        public PairingServer()
        {
            string ip = GetLocalIpAddress();
            int port = FindAvailablePort();
            string token = Guid.NewGuid().ToString();
            string deviceName = Environment.MachineName;

            Info = new PairingInfo
            {
                Ip = ip,
                Port = port,
                Token = token,
                DeviceName = deviceName
            };

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
                    _ = HandleClientAsync(client, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Pairing server error: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            try
            {
                string token = await reader.ReadLineAsync();
                Debug.WriteLine($"Received pairing token: {token}");

                if (token?.Trim() == Info.Token)
                {
                    await writer.WriteLineAsync("SEND_ID");
                    string deviceId = await reader.ReadLineAsync();
                    Debug.WriteLine(deviceId);

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
                        _deviceManager.AddDevice(deviceId, deviceName, remoteIp);

                        Debug.WriteLine($"Device paired: {deviceName} ({deviceId})");
                        await writer.WriteLineAsync("PAIRING_SUCCESS");
                        DevicePaired?.Invoke(deviceName);
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

        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }
}