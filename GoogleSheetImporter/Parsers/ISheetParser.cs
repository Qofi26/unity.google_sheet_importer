using System.Collections.Generic;

namespace GoogleSheetImporter.Parsers
{
    public interface ISheetParser
    {
        public object Parse(IList<IList<object>> values);
    }
}
