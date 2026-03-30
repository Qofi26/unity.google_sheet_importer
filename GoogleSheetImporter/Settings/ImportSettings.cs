using System;
using System.Collections.Generic;

namespace GoogleSheetImporter.Settings
{
    [Serializable]
    internal sealed class ImportSettings
    {
        public string RootFolderUrlOrId = "";
        public string NamePrefix = "#";

        public string OutputFolder;
        public int SelectedFolderIndex = -1;

        public readonly AuthSettings AuthSettings = new();
        public readonly List<GDriveFolder> Folders = new();
        public readonly Dictionary<string, bool> SelectedFiles = new();
        public readonly List<GDriveFile> Spreadsheets = new();
    }
}
