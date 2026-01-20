using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using GoogleSheetImporter.Settings;

namespace GoogleSheetImporter.Services
{
    internal sealed class GoogleAuthService
    {
        private static readonly string[] _scopes =
        {
            DriveService.Scope.DriveReadonly,
            SheetsService.Scope.SpreadsheetsReadonly
        };

        private readonly IAuthSettings _settings;
        private ICredential _credential;

        public GoogleAuthService(IAuthSettings settings)
        {
            _settings = settings;
        }

        public async void Authorize()
        {
            switch (_settings.Mode)
            {
                case AuthMode.OAuthInstalledApp:
                    _credential = await AuthorizeOAuthInstalledApp(_settings.ClientId, _settings.ClientSecret);
                    break;
                case AuthMode.ServiceAccount:
                    _credential = AuthorizeServiceAccount(_settings.ServiceAccountKeyPath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public DriveService CreateDriveService()
        {
            EnsureAuthorized();
            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = "Unity Google Drive Sheets Importer"
            });
        }

        public SheetsService CreateSheetsService()
        {
            EnsureAuthorized();
            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = "Unity Google Drive Sheets Importer"
            });
        }

        private void EnsureAuthorized()
        {
            if (_credential == null)
            {
                throw new InvalidOperationException("Не выполнена авторизация. Нажмите 'Authorize'.");
            }
        }

        private static async Task<ICredential> AuthorizeOAuthInstalledApp(string clientId, string clientSecret)
        {
            var secrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret };

            var tokenDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "UnityGoogleImporter",
                "OAuthTokens");

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                _scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(tokenDir, true)
            );

            return credential;
        }

        private static ICredential AuthorizeServiceAccount(string serviceAccountKeyPath)
        {
            if (string.IsNullOrWhiteSpace(serviceAccountKeyPath) || !File.Exists(serviceAccountKeyPath))
            {
                throw new FileNotFoundException("Не найден service_account_key.json", serviceAccountKeyPath);
            }

            using var stream = new FileStream(serviceAccountKeyPath, FileMode.Open, FileAccess.Read);
            var credential = GoogleCredential.FromStream(stream).CreateScoped(_scopes);

            return credential;
        }
    }
}
