using Newtonsoft.Json.Linq;

namespace GoogleSheetImporter.Parsers
{
    public interface ICellParser
    {
        public JToken GetCellValue(object cell);
        public object ParseCell(object cell);
    }
}
