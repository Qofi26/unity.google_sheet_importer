using UnityEngine;

namespace GoogleSheetImporter.Parsers
{
    public abstract class AbstractSheetParserProvider : ScriptableObject, ISheetParserProvider
    {
        public abstract ISheetParser GetParser(string sheetTitle);
    }
}
