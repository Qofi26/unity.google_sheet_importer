using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GoogleSheetImporter.Mappers
{
    public class DefaultPropertySetter : IConfigPropertySetter
    {
        private readonly Object _target;
        private readonly bool _formatted;
        private readonly IReadOnlyDictionary<string, string> _fieldsMap;

        public DefaultPropertySetter(Object target, bool formattedJson, IReadOnlyDictionary<string, string> fieldsMap)
        {
            _target = target;
            _formatted = formattedJson;
            _fieldsMap = fieldsMap;
        }

        public void Apply(JToken token)
        {
            PopulateObject(token, _target, _formatted);
            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssetIfDirty(_target);
            AssetDatabase.Refresh();
        }

        private void PopulateObject(JToken jObject, object target, bool formatted)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new PrivateResolver(),
                Converters = { new FlexibleFloatConverter() },
                Culture = CultureInfo.InvariantCulture,
                Formatting = formatted
                    ? Formatting.Indented
                    : Formatting.None
            };

            var targetType = target.GetType();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var property in targetType.GetProperties(flags))
            {
                if (!property.CanWrite)
                {
                    continue;
                }

                var propertyName = property.Name;
                var jsonKey = propertyName;

                if (_fieldsMap.TryGetValue(propertyName, out var newJsonKey))
                {
                    jsonKey = newJsonKey;
                }

                var jToken = jObject[jsonKey];
                if (jToken == null)
                {
                    continue;
                }

                try
                {
                    var value = CreatePropertyValue(property, jToken, settings);
                    property.SetValue(target, value);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"Failed applying property {property.Name} with type {property.PropertyType}. Exception: {e}");
                }
            }
        }

        private static object CreatePropertyValue(
            PropertyInfo property,
            JToken jToken,
            JsonSerializerSettings settings)
        {
            var propertyType = property.PropertyType;

            if (propertyType == typeof(string))
            {
                var formatting = settings.Formatting;
                return jToken.ToString(formatting);
            }

            var value = jToken.ToString();
            var propertyValue = JsonConvert.DeserializeObject(value, propertyType, settings);

            return propertyValue;
        }

        private class PrivateResolver : DefaultContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

                var members = new List<MemberInfo>();
                members.AddRange(objectType.GetFields(flags));
                members.AddRange(objectType.GetProperties(flags));

                return members;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);

                if (member is FieldInfo or PropertyInfo)
                {
                    prop.Writable = true;
                    prop.Readable = true;
                }

                return prop;
            }
        }
    }
}
