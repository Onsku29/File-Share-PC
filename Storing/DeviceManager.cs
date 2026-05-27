using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

namespace FileShare.Storing
{
    public class PairedDevice(string deviceId, string deviceName, string deviceIp)
    {
        public string DeviceId { get; set; } = deviceId;
        public string DeviceName { get; set; } = deviceName;
        public string DeviceIp { get; set; } = deviceIp;
    }

    public class DeviceManager
    {
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileShare", "paired_devices.json");
        private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
        private List<PairedDevice> _pairedDevices;
        public event Action<PairedDevice>? DevicePaired;

        public DeviceManager()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            _pairedDevices = LoadDevices();
        }


        private static List<PairedDevice> LoadDevices()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<List<PairedDevice>>(json) ?? [];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load paired devices: {ex.Message}");
            }

            return [];
        }

        private void SaveDevices()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                string json = JsonSerializer.Serialize(_pairedDevices, jsonSerializerOptions);
                using StreamWriter writer = File.CreateText(FilePath);
                writer.Write(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save paired devices: {ex.Message}");
            }
        }

        public bool IsDevicePaired(string deviceId)
        {
            return _pairedDevices.Exists(d => d.DeviceId == deviceId);
        }

        public bool IsDevicePaired(PairedDevice device)
        {
            return _pairedDevices.Contains(device);
        }

        public void AddDevice(PairedDevice device)
        {
            _pairedDevices.Add(device);
            DevicePaired?.Invoke(device);
            SaveDevices();
        }

        public void DeleteDevice(PairedDevice device)
        {
            if (device != null)
            {
                Debug.WriteLine($"Deleting device: {device.DeviceName}");
                _pairedDevices.Remove(device);
                SaveDevices();
                _pairedDevices = LoadDevices();
            }
        }

        public IReadOnlyList<PairedDevice> GetPairedDevices()
        {
            return _pairedDevices.AsReadOnly();
        }
    }
}
