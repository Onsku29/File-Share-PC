using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace File_Share.Storing
{
    // AVAILABLE PREFERENCES:
    // PickIndividualLocations - boolean, if true the user picks saving location for every file separately, if false all files are saved to the same folder

    class PreferenceManager
    {
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileShare", "preferences.json");
        private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        public PreferenceManager()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        }
    }
}
