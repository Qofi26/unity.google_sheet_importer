using UnityEngine;

namespace GoogleSheetImporter.Mappers
{
    public abstract class AbstractConfigMapperProvider : ScriptableObject, IConfigMapperProvider
    {
        public abstract Object GetTarget();
        public abstract string GetDisplayName();
        public abstract IConfigMapper GetMapper();
    }
}
