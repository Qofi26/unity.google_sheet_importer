using Newtonsoft.Json.Linq;

namespace GoogleSheetImporter.Mappers
{
    public interface IConfigModifier
    {
        public void Modify(JToken token);
    }
}
