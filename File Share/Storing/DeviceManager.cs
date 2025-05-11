using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FileShare.Storing
{
    public class PairedDevice
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceIp { get; set; }
    }

    class DeviceManager
    {
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileShare", "paired_devices.json");
        private List<PairedDevice> _pairedDevices;

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

        public void AddDevice(string deviceId, string deviceName, string deviceIp)
        {
            if (!IsDevicePaired(deviceId))
            {
                _pairedDevices.Add(new PairedDevice
                {
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    DeviceIp = deviceIp
                });

                SaveDevices();
            }
        }

        public IReadOnlyList<PairedDevice> GetAllPairedDevices()
        {
            return _pairedDevices.AsReadOnly();
        }
    }
}
