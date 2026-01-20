using Newtonsoft.Json.Linq;

namespace GoogleSheetImporter.Mappers
{
    public interface IConfigTokenProvider
    {
        public JToken GetToken();
    }
}
