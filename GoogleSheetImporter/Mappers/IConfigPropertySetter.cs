using Newtonsoft.Json.Linq;

namespace GoogleSheetImporter.Mappers
{
    public interface IConfigPropertySetter
    {
        public void Apply(JToken token);
    }
}
