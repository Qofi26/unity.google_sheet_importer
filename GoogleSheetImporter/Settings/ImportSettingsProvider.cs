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

        public static void Save(ImportSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            var json = JsonConvert.SerializeObject(settings);
            PlayerPrefs.SetString(kPrefsKey, json);
        }
    }
}
