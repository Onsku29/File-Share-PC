using FileShare.Networking;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace FileShare.Storing
{
    class ServerInfoManager
    {
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileShare", "server_info.json");
        private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        public ServerInfoManager()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        }

        public static string GetServerToken()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var serverInfo = JsonSerializer.Deserialize<PairingInfo>(json);
                    return serverInfo?.Token ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load server token: {ex.Message}");
            }
            return string.Empty;
        }

        public int GetServerPort()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var serverInfo = JsonSerializer.Deserialize<PairingInfo>(json);
                    return serverInfo?.Port ?? 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load server port: {ex.Message}");
            }
            return 0;
        }

        public string GetServerName()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var serverInfo = JsonSerializer.Deserialize<PairingInfo>(json);
                    return serverInfo?.DeviceName ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load server name: {ex.Message}");
            }
            return string.Empty;
        }

        public string GetServerIP()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var serverInfo = JsonSerializer.Deserialize<PairingInfo>(json);
                    return serverInfo?.Ip ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load server IP: {ex.Message}");
            }
            return string.Empty;
        }

        public static void SaveServerInfo(PairingInfo info)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                string json = JsonSerializer.Serialize(info, jsonSerializerOptions);
                using StreamWriter writer = File.CreateText(FilePath);
                writer.Write(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save pairing info: {ex.Message}");
            }
        }
    }
}