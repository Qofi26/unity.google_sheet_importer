namespace GoogleSheetImporter.Parsers
{
    public interface ISheetParserProvider
    {
        public ISheetParser GetParser(string title);
    }
}
