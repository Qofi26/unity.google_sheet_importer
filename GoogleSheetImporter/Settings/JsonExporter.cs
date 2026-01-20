using System.IO;
using Newtonsoft.Json;
using UnityEditor;

namespace GoogleSheetImporter
{
    internal static class JsonExporter
    {
        public static readonly JsonSerializerSettings SaveSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.Indented,
        };

        public static void Save(object data, string path)
        {
            var settings = SaveSettings;

            var json = JsonConvert.SerializeObject(data, settings);
            var dir = Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(dir))
            {
                throw new IOException("Invalid path: " + path);
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }

        public static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return name;
        }
    }
}
