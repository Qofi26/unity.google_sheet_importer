using System.IO;
using Newtonsoft.Json.Linq;

namespace GoogleSheetImporter.Mappers
{
    public class FileConfigTokenProvider : IConfigTokenProvider
    {
        private readonly string _filePath;

        public FileConfigTokenProvider(string filePath)
        {
            _filePath = filePath;
        }

        public JToken GetToken()
        {
            var json = File.ReadAllText(_filePath);
            var token = JObject.Parse(json);
            return token;
        }
    }
}
