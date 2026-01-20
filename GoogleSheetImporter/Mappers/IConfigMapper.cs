namespace GoogleSheetImporter.Mappers
{
    public interface IConfigMapper
    {
        public void Apply();
    }

    public interface IConfigMapperProvider
    {
        public IConfigMapper GetMapper();
    }
}
