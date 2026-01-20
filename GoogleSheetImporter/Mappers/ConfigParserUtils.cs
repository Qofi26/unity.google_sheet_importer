using System.Linq;
using Newtonsoft.Json.Linq;

namespace GoogleSheetImporter.Mappers
{
    public static class ConfigParserUtils
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
    }
}
