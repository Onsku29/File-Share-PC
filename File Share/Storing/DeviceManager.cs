using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

namespace FileShare.Storing
{
    public class PairedDevice
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceIp { get; set; }

        public PairedDevice(string deviceId, string deviceName, string deviceIp)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            DeviceIp = deviceIp;
        }
    }

    public class DeviceManager
    {
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileShare", "paired_devices.json");
        private List<PairedDevice> _pairedDevices;
        public event Action<PairedDevice>? DevicePaired;

        public DeviceManager()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            _pairedDevices = LoadDevices();
        }


        private List<PairedDevice> LoadDevices()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<List<PairedDevice>>(json) ?? new List<PairedDevice>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load paired devices: {ex.Message}");
            }

            return new List<PairedDevice>();
        }

        private void SaveDevices()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                string json = JsonSerializer.Serialize(_pairedDevices, new JsonSerializerOptions { WriteIndented = true });
                using (StreamWriter writer = File.CreateText(FilePath))
                {
                    writer.Write(json);
                }
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

        public IReadOnlyList<PairedDevice> GetAllPairedDevices()
        {
            return _pairedDevices.AsReadOnly();
        }
    }
}
