using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GoogleSheetImporter.Parsers
{
    public sealed class DefaultSheetParser : ISheetParser
    {
        [Serializable]
        public class Settings
        {
            public int HeaderCount;
        }

        private readonly Settings _settings;

        public DefaultSheetParser(Settings settings)
        {
            _settings = settings;
        }

        public object Parse(IList<IList<object>> values)
        {
            if (_settings.HeaderCount <= 0)
            {
                return ParseKeyValue(values);
            }

            return ParseTable(values, _settings);
        }

        private static Dictionary<string, object> ParseKeyValue(IEnumerable<IList<object>> values)
        {
            var result = new Dictionary<string, object>();

            foreach (var row in values)
            {
                if (row.Count == 0)
                {
                    continue;
                }

                var key = row[0]?.ToString() ?? "";

                if (row.Count == 2)
                {
                    var value = GetCellValue(row[1]);
                    result[key] = value;
                }
                else if (row.Count > 2)
                {
                    var obj = new Dictionary<string, object>();
                    for (var i = 1; i < row.Count; i++)
                    {
                        var value = GetCellValue(row[i]);
                        obj[$"col{i}"] = value;
                    }

                    result[key] = obj;
                }
            }

            return result;
        }

        private static List<object> ParseTable(IList<IList<object>> values, Settings settings)
        {
            var headerCount = settings.HeaderCount;
            if (values.Count <= headerCount)
            {
                return new List<object>();
            }

            var headers = GetHeaders(values, headerCount);

            var data = new List<object>();
            foreach (var row in values.Skip(headerCount))
            {
                var jObject = new JObject();

                for (var i = 0; i < row.Count; i++)
                {
                    var cell = row[i];
                    var header = headers[i];
                    var token = GetCellValue(cell);

                    SetValue(jObject, header, token);
                }

                data.Add(jObject);
                RemoveNullProperties(jObject);
            }

            return data;
        }

        private static JToken GetCellValue(object cell)
        {
            var value = ParseCell(cell);
            JToken token;

            if (value == null || string.IsNullOrEmpty(cell.ToString()))
            {
                token = null;
            }
            else if (value is JArray array)
            {
                token = array;
            }
            else
            {
                token = JToken.FromObject(value);
            }

            return token;
        }

        public static void RemoveNullProperties(JObject obj)
        {
            var properties = obj.Properties().ToList();
            foreach (var property in properties)
            {
                if (property.Value.Type == JTokenType.Null)
                {
                    property.Remove();
                }
                else if (property.Value.Type == JTokenType.Object)
                {
                    RemoveNullProperties((JObject) property.Value);
                }

                if (property.Value.ToString() == "{}")
                {
                    property.Remove();
                }
            }
        }

        public static object ParseCell(object cell)
        {
            if (cell == null)
            {
                return null;
            }

            var value = cell;

            var stringVal = cell.ToString();

            if (int.TryParse(stringVal, out var intVal))
            {
                value = intVal;
            }
            else if (float.TryParse(stringVal, out var floatVal))
            {
                value = floatVal;
            }
            else if (bool.TryParse(stringVal, out var boolVal))
            {
                value = boolVal;
            }
            else
            {
                try
                {
                    value = JArray.Parse(stringVal);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return value;
        }

        private static void SetValue(JObject jObject, HeaderConfig header, JToken token)
        {
            if (header.Parent == null)
            {
                jObject[header.Name] = token;
                return;
            }

            var targetObject = GetTargetObject(jObject, header, header.Name);
            targetObject[header.Name] = token;
        }

        private static JObject GetTargetObject(JObject jObject, HeaderConfig header, string origin)
        {
            if (header.Parent == null)
            {
                if (!jObject.ContainsKey(header.Name))
                {
                    jObject[header.Name] = new JObject();
                }

                return (JObject) jObject[header.Name];
            }

            var targetObject = GetTargetObject(jObject, header.Parent, origin);
            if (!targetObject.ContainsKey(header.Name))
            {
                targetObject[header.Name] = new JObject();
            }

            if (header.Name == origin)
            {
                return targetObject;
            }

            return (JObject) targetObject[header.Name];
        }

        private static Dictionary<int, HeaderConfig> GetHeaders(IList<IList<object>> values, int headerLevelCount)
        {
            var result = new Dictionary<int, HeaderConfig>();

            var maxColumn = values.Max(x => x.Count);
            for (var headerLevel = 0; headerLevel < headerLevelCount; headerLevel++)
            {
                var row = values[headerLevel];
                var headerName = string.Empty;
                for (var index = 0; index < maxColumn; index++)
                {
                    if (index < row.Count)
                    {
                        var newHeader = row[index].ToString();
                        if (!string.IsNullOrEmpty(newHeader))
                        {
                            headerName = newHeader;
                        }
                    }

                    if (string.IsNullOrEmpty(headerName))
                    {
                        continue;
                    }

                    if (!result.TryGetValue(index, out var header))
                    {
                        header = new HeaderConfig(headerName);
                    }
                    else
                    {
                        header = new HeaderConfig(headerName, header);
                    }

                    result[index] = header;
                }
            }

            return result;
        }
    }

    internal class HeaderConfig
    {
        public readonly string Name;
        public readonly HeaderConfig Parent;

        public HeaderConfig(string name)
        {
            Name = name;
            Parent = null;
        }

        public HeaderConfig(string name, HeaderConfig parent)
        {
            Name = name;
            Parent = parent;
        }

        public JObject CreateJObject()
        {
            var current = new JObject();

            var node = this;
            while (node != null)
            {
                var wrapper = new JObject
                {
                    [node.Name] = current
                };

                current = wrapper;
                node = node.Parent;
            }

            return current;
        }
    }
}
