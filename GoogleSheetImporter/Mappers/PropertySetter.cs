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
    public class PropertySetter : IConfigPropertySetter
    {
        private readonly Object _target;
        private readonly bool _formatted;

        public PropertySetter(Object target, bool formattedJson)
        {
            _target = target;
            _formatted = formattedJson;
        }

        public void Apply(JToken token)
        {
            PopulateObject(token, _target, _formatted);
            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssetIfDirty(_target);
            AssetDatabase.Refresh();
        }

        private static void PopulateObject(JToken jObject, object target, bool formatted)
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

                var jToken = jObject[property.Name];
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
