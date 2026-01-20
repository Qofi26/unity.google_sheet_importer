using System;
using Newtonsoft.Json.Linq;

namespace GoogleSheetImporter.Parsers
{
    public class DefaultCellParser : ICellParser
    {
        public JToken GetCellValue(object cell)
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

        public object ParseCell(object cell)
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
    }
}
