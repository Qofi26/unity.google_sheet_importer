using System;
using System.Collections.Generic;
using Google.Apis.Drive.v3;

namespace GoogleSheetImporter.Wrappers
{
    internal sealed class DriveServiceWrapper
    {
        private readonly DriveService _drive;

        public DriveServiceWrapper(DriveService drive)
        {
            _drive = drive;
        }

        /// <summary>
        /// Принимает полный URL папки или чистый ID. Возвращает ID.
        /// Поддерживает форматы:
        /// - https://drive.google.com/drive/folders/{ID}
        /// - https://drive.google.com/open?id={ID}
        /// - просто {ID}
        /// </summary>
        public static string ExtractFolderId(string urlOrId)
        {
            if (string.IsNullOrWhiteSpace(urlOrId))
            {
                throw new ArgumentException("Пустая строка");
            }

            if (!urlOrId.Contains("drive.google.com"))
            {
                return urlOrId.Trim();
            }

            var id = urlOrId;
            var idx = id.IndexOf("/folders/", StringComparison.Ordinal);
            if (idx >= 0)
            {
                id = id.Substring(idx + "/folders/".Length);
                var q = id.IndexOfAny(new[] { '?', '/', '#' });
                if (q >= 0) id = id.Substring(0, q);
                return id;
            }

            // ?id=
            idx = id.IndexOf("id=", StringComparison.Ordinal);
            if (idx >= 0)
            {
                id = id.Substring(idx + 3);
                var q = id.IndexOfAny(new[] { '&', '/', '#' });
                if (q >= 0) id = id.Substring(0, q);
                return id;
            }

            throw new ArgumentException("Не удалось извлечь ID папки из URL");
        }

        public List<GDriveFolder> ListSubfolders(string parentFolderId, string namePrefix)
        {
            var result = new List<GDriveFolder>();
            string pageToken = null;

            do
            {
                var request = _drive.Files.List();
                request.Q =
                    $"'{parentFolderId}' in parents and " +
                    $"mimeType = 'application/vnd.google-apps.folder' and " +
                    $"trashed = false";
                request.Fields = "files(id,name),nextPageToken";
                request.PageSize = 1000;
                request.SupportsAllDrives = true;
                request.IncludeItemsFromAllDrives = true;

                var resp = request.Execute();
                foreach (var f in resp.Files)
                {
                    if (!string.IsNullOrEmpty(namePrefix) && !f.Name.StartsWith(namePrefix))
                    {
                        continue;
                    }

                    result.Add(new GDriveFolder { Id = f.Id, Name = f.Name });
                }

                pageToken = resp.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            return result;
        }

        public List<GDriveFile> ListSpreadsheetsInFolder(string folderId, string namePrefix = null)
        {
            var result = new List<GDriveFile>();
            string pageToken = null;

            do
            {
                var request = _drive.Files.List();
                request.Q =
                    $"'{folderId}' in parents and " +
                    $"mimeType = 'application/vnd.google-apps.spreadsheet' and " +
                    $"trashed = false";
                request.Fields = "files(id,name,webViewLink),nextPageToken";
                request.PageSize = 1000;
                request.SupportsAllDrives = true;
                request.IncludeItemsFromAllDrives = true;

                var resp = request.Execute();
                foreach (var file in resp.Files)
                {
                    if (!string.IsNullOrEmpty(namePrefix) && !file.Name.StartsWith(namePrefix))
                    {
                        continue;
                    }

                    result.Add(new GDriveFile
                    {
                        Id = file.Id,
                        Name = file.Name,
                        WebViewLink = file.WebViewLink
                    });
                }

                pageToken = resp.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            return result;
        }
    }
}
