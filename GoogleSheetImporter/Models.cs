using System;

namespace GoogleSheetImporter
{
    [Serializable]
    internal sealed class GDriveFolder
    {
        public string Id;
        public string Name;
    }

    [Serializable]
    internal sealed class GDriveFile
    {
        public string Id;
        public string Name;
        public string WebViewLink;
    }

    internal enum AuthMode
    {
        OAuthInstalledApp = 0,
        ServiceAccount = 1
    }
}
