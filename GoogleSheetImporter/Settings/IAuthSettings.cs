namespace GoogleSheetImporter.Settings
{
    internal interface IAuthSettings
    {
        public AuthMode AuthMode { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ServiceAccountKeyPath { get; set; }
    }
}
