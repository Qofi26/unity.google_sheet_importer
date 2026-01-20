using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;

namespace GoogleSheetImporter.Settings
{
    internal sealed class ImportSettings
    {
        private const string kEditorPrefsKey = "GoogleSheetImporter_ImportSettings_v1";
        private const string kDefaultOutputFolder = "ConfigAssets";

        public string RootFolderUrlOrId = "";
        public string NamePrefix = "#";

        public string OutputFolder = kDefaultOutputFolder;

        public readonly List<GDriveFolder> Folders = new();
        public readonly Dictionary<string, bool> SelectedFiles = new();
        public readonly List<GDriveFile> Spreadsheets = new();

        internal static ConfigSettings ConfigSettings { get; private set; }

        public int SelectedFolderIndex = -1;

        public static ImportSettings Load()
        {
            var json = EditorPrefs.GetString(kEditorPrefsKey, "");
            if (string.IsNullOrEmpty(json))
            {
                return new ImportSettings();
            }

            try
            {
                return JsonConvert.DeserializeObject<ImportSettings>(json) ?? new ImportSettings();
            }
            catch
            {
                return new ImportSettings();
            }

        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this);
            EditorPrefs.SetString(kEditorPrefsKey, json);
        }

        public void EnsureOutputFolder()
        {
            if (string.IsNullOrEmpty(OutputFolder))
            {
                OutputFolder = kDefaultOutputFolder;
            }

            if (Directory.Exists(OutputFolder))
            {
                return;
            }

            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
        }
    }
}
