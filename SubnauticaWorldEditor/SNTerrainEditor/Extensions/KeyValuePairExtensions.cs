using System.Collections.Generic;

namespace SNTerrainEditor.Extensions;

public static class KeyValuePairExtensions
{
    extension<TKey, TValue>(KeyValuePair<TKey, TValue> kvp)
    {
        public void Deconstruct(out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}