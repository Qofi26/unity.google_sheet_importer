namespace GoogleSheetImporter.Settings
{
    internal interface IAuthSettings
    {
        public AuthMode Mode { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ServiceAccountKeyPath { get; set; }
    }
}
