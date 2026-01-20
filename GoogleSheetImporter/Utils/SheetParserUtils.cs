using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GoogleSheetImporter.Parsers
{
    public static class SheetParserUtils
    {
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

        public static Dictionary<int, HeaderConfig> GetHeaders(IList<IList<object>> values, int headerLevelCount)
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

        public static void SetValue(JObject jObject, HeaderConfig header, JToken token)
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
    }

    public class HeaderConfig
    {
        public readonly string Name;
        public readonly HeaderConfig Parent;

        public HeaderConfig(string name, HeaderConfig parent = null)
        {
            Name = name;
            Parent = parent;
        }
    }
}
