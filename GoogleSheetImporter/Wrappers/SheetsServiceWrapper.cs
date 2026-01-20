using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace GoogleSheetImporter.Wrappers
{
    internal sealed class SheetsServiceWrapper
    {
        private readonly SheetsService _sheets;

        public SheetsServiceWrapper(SheetsService sheets)
        {
            _sheets = sheets;
        }

        public (string SpreadsheetName, IList<Sheet> Sheets) GetSpreadsheetMeta(string spreadsheetId)
        {
            var req = _sheets.Spreadsheets.Get(spreadsheetId);
            req.Fields = "properties.title,sheets(properties(title))";
            var meta = req.Execute();
            return (meta.Properties.Title, meta.Sheets);
        }

        public IList<IList<object>> GetValues(string spreadsheetId, string sheetTitle)
        {
            var range = $"{Escape(sheetTitle)}!A:ZZZ";
            var req = _sheets.Spreadsheets.Values.Get(spreadsheetId, range);
            var resp = req.Execute();
            return resp.Values ?? Array.Empty<IList<object>>();
        }

        private static string Escape(string title)
        {
            return $"'{title.Replace("'", "''")}'";
        }
    }
}
