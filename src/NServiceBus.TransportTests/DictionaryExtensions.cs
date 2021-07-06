namespace NServiceBus.TransportTests
{
    using System.Collections.Generic;

    static class DictionaryExtensions
    {
        public static bool Contains(this IDictionary<string, string> dictionary, string key, string value) =>
            dictionary.ContainsKey(key) && dictionary[key] == value;

        public static bool Contains(this IReadOnlyDictionary<string, string> dictionary, string key, string value) =>
            dictionary.ContainsKey(key) && dictionary[key] == value;
    }
}
