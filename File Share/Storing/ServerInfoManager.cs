using FileShare.Networking;
using FileShare.Storing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileShare.Storing
{
    class ServerInfoManager
    {
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileShare", "server_info.json");

        public ServerInfoManager()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        }

        public string GetServerToken()
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

        public void SaveServerInfo(PairingInfo info)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                string json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
                using (StreamWriter writer = File.CreateText(FilePath))
                {
                    writer.Write(json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save pairing info: {ex.Message}");
            }
        }
    }
}