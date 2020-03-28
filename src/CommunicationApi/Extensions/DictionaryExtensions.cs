using System.Collections.Generic;

namespace CommunicationApi.Extensions
{
    public static class DictionaryExtensions
    {
        public static void Upsert<K, V>(this IDictionary<K, V> dictionary, K key, V value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }
    }
}