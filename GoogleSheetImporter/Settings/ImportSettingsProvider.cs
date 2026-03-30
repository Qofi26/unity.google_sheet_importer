using Newtonsoft.Json;
using UnityEngine;

namespace GoogleSheetImporter.Settings
{
    internal static class ImportSettingsProvider
    {
        public const string kDefaultOutputFolder = "ConfigAssets";

        private const string kPrefsKey = "GoogleSheetImporter_ImportSettings_v1";

        public static ImportSettings Load()
        {
            var json = PlayerPrefs.GetString(kPrefsKey, "");
            var settings = Deserialize(json);
            return settings;
        }

        public static void Save(ImportSettings settings)
        {
            var json = Serialize(settings);
            PlayerPrefs.SetString(kPrefsKey, json);
        }

        public static string Serialize(ImportSettings settings)
        {
            if (settings == null)
            {
                return string.Empty;
            }

            var json = JsonConvert.SerializeObject(settings);
            return json;
        }

        public static ImportSettings Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new ImportSettings();
            }

            try
            {
                var settings = JsonConvert.DeserializeObject<ImportSettings>(json) ?? new ImportSettings();

                if (string.IsNullOrWhiteSpace(settings.OutputFolder))
                {
                    settings.OutputFolder = kDefaultOutputFolder;
                }

                return settings;
            }
            catch
            {
                return new ImportSettings();
            }
        }
    }
}
