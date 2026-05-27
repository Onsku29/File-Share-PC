using FileShare.Storing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace File_Share.Storing
{
    // AVAILABLE PREFERENCES:
    // PickIndividualLocations - boolean, false by default; if true the user picks saving location for every file separately, if false all files are saved to the same folder

    static class PreferenceManager
    {
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileShare", "preferences.json");
        private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
        private static Dictionary<string, JsonElement> _prefs = [];

        // DEFAULT PREFERENCES
        private static readonly Dictionary<string, JsonElement> _defaultPrefs = new Dictionary<string, JsonElement>
        {
            {"PickIndividualLocations", JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(false)).RootElement.Clone()}
        };

        static PreferenceManager()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            InitPrefs();
        }

        private static void InitPrefs()
        {
            _prefs = LoadPrefs();

            foreach (KeyValuePair<string, JsonElement> pref in _defaultPrefs)
            {
                if (!_prefs.ContainsKey(pref.Key))
                {
                    _prefs.Add(pref.Key, pref.Value);
                }
            }

            SavePrefs();
        }

        private static void SavePrefs()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                string json = JsonSerializer.Serialize(_prefs, jsonSerializerOptions);
                using StreamWriter writer = File.CreateText(FilePath);
                writer.Write(json);
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save preferences: {ex.Message}");
            }
        }

        private static Dictionary<string, JsonElement> LoadPrefs()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load paired devices: {ex.Message}");
            }

            return [];
        }
    }
}
