using System;
using System.Collections.Generic;
using System.Linq;
using GoogleSheetImporter.Mappers;
using GoogleSheetImporter.Parsers;
using UnityEditor;
using UnityEngine;

namespace GoogleSheetImporter.Settings
{
    [CreateAssetMenu(fileName = nameof(GoogleSheetImporterSettings),
        menuName = nameof(GoogleSheetImporter) + "/Settings",
        order = 0)]
    internal sealed class GoogleSheetImporterSettings : ScriptableObject, IAuthSettings
    {
        private static GoogleSheetImporterSettings _instance;

        public static GoogleSheetImporterSettings Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindAsset();
                }

                return _instance;
            }
        }

        [field: SerializeField] public string ClientId { get; set; } = string.Empty;
        [field: SerializeField] public string ClientSecret { get; set; } = string.Empty;

        [field: SerializeField] public AuthMode Mode { get; set; } = AuthMode.OAuthInstalledApp;
        [field: SerializeField] public string ServiceAccountKeyPath { get; set; } = string.Empty;

        [SerializeField]
        private List<ParserConfig> _parsers = new();

        [SerializeField]
        private List<MappingConfig> _mappers = new();

        public List<MappingConfig> Mappers => _mappers;

        public bool TryGetParserProvider(string fileName, out AbstractSheetParserProvider provider)
        {
            var config = _parsers.FirstOrDefault(x => x.Id == fileName);
            provider = config?.Parser;
            return provider;
        }

        public void SetParserProvider(string fileName, AbstractSheetParserProvider provider)
        {
            var index = _parsers.FindIndex(x => x.Id == fileName);
            if (index < 0)
            {
                _parsers.Add(new ParserConfig(fileName, provider));
            }
            else
            {
                _parsers[index].Parser = provider;
            }

            EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            for (int i = _mappers.Count - 1; i >= 0; i--)
            {
                if (_mappers[i] == null || _mappers[i].MapperProvider == null)
                {
                    _mappers.RemoveAt(i);
                }
            }

            for (int i = _parsers.Count - 1; i >= 0; i--)
            {
                if (_parsers[i] == null || _parsers[i].Parser == null)
                {
                    _parsers.RemoveAt(i);
                }
            }
        }

        private static GoogleSheetImporterSettings FindAsset()
        {
            var assets = AssetDatabase.FindAssets($"t:{typeof(GoogleSheetImporterSettings)}");
            var guid = assets.FirstOrDefault();

            if (string.IsNullOrEmpty(guid))
            {
                throw new Exception($"No asset of type {typeof(GoogleSheetImporterSettings)} found in the project. "
                                    + $"Please create one using the Create Asset menu.");
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<GoogleSheetImporterSettings>(path);
        }

        [Serializable]
        private class ParserConfig
        {
            [field: SerializeField] public string Id { get; private set; }

            [field: SerializeField] public AbstractSheetParserProvider Parser { get; set; }

            public ParserConfig(string id, AbstractSheetParserProvider parser)
            {
                Id = id;
                Parser = parser;
            }
        }

        [Serializable]
        public class MappingConfig
        {
            [field: SerializeField] public AbstractConfigMapperProvider MapperProvider { get; set; }
            [field: SerializeField] public bool Selected { get; set; }

            public MappingConfig() { }

            public MappingConfig(AbstractConfigMapperProvider mapperProvider)
            {
                MapperProvider = mapperProvider;
            }
        }
    }
}
