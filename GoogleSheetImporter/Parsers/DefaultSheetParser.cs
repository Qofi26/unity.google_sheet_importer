using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace GoogleSheetImporter.Parsers
{
    [Preserve]
    public sealed class DefaultSheetParser : ISheetParser
    {
        [Serializable]
        public class Settings
        {
            public bool ImportAsConstants = false;

            public int HeaderLevels = 1;

            public int ConstantKeyColumnIndex = 0;
            public int[] ConstantValueColumnIndex = { 1 };
            public bool UseConstantValueAsSingle = true;
        }

        private readonly Settings _settings;
        private readonly ICellParser _cellParser;

        public DefaultSheetParser(Settings settings = null)
        {
            settings ??= new Settings();

            _settings = settings;

            _cellParser = new DefaultCellParser();
        }

        public object Parse(IList<IList<object>> values)
        {
            if (_settings.ImportAsConstants)
            {
                return ParseKeyValue(_settings, values);
            }

            return ParseTable(_settings, values);
        }

        private Dictionary<string, object> ParseKeyValue(Settings settings, IEnumerable<IList<object>> values)
        {
            var result = new Dictionary<string, object>();

            var keyColumnIndex = settings.ConstantKeyColumnIndex;

            foreach (var row in values)
            {
                if (row.Count == 0)
                {
                    continue;
                }

                var key = row[keyColumnIndex]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (settings.UseConstantValueAsSingle)
                {
                    var valueColumnIndex = settings.ConstantValueColumnIndex.FirstOrDefault();
                    var value = GetCellValue(row[valueColumnIndex]);
                    result[key] = value;
                }
                else
                {
                    var obj = new Dictionary<string, object>();
                    foreach (var index in settings.ConstantValueColumnIndex)
                    {
                        if (index >= row.Count)
                        {
                            continue;
                        }

                        var rowValue = row[index];

                        var value = GetCellValue(rowValue);
                        obj[$"col{index}"] = value;
                    }

                    result[key] = obj;
                }
            }

            return result;
        }

        private List<object> ParseTable(Settings settings, IList<IList<object>> values)
        {
            var headerCount = settings.HeaderLevels;
            if (values.Count <= headerCount)
            {
                return new List<object>();
            }

            var headers = SheetParserUtils.GetHeaders(values, headerCount);

            var data = new List<object>();
            foreach (var row in values.Skip(headerCount))
            {
                var jObject = new JObject();

                for (var i = 0; i < row.Count; i++)
                {
                    var cell = row[i];
                    var header = headers[i];
                    var token = GetCellValue(cell);

                    SheetParserUtils.SetValue(jObject, header, token);
                }

                data.Add(jObject);
                SheetParserUtils.RemoveNullProperties(jObject);
            }

            return data;
        }

        private JToken GetCellValue(object cell)
        {
            return _cellParser.GetCellValue(cell);
        }
    }
}
