using System;
using System.Globalization;
using Newtonsoft.Json;

namespace GoogleSheetImporter.Mappers
{
    public class FlexibleFloatConverter : JsonConverter<float>
    {
        public override float ReadJson(
            JsonReader reader,
            Type objectType,
            float existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.Value == null)
                return 0f;

            var str = reader.Value.ToString();

            if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            if (float.TryParse(str, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
            {
                return result;
            }

            throw new JsonSerializationException($"Не удалось преобразовать '{str}' в float");
        }

        public override void WriteJson(JsonWriter writer, float value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
