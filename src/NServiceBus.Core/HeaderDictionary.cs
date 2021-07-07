// unset

#pragma warning disable 1591
namespace NServiceBus
{
    using System.Collections.Generic;

    /// <summary>
    ///
    /// </summary>
    public class HeaderDictionary
    {
        IDictionary<string, string> incomingHeaders;

        public HeaderDictionary()
        {
            this.incomingHeaders = new Dictionary<string, string>();
        }

        public HeaderDictionary(IDictionary<string, string> incomingHeaders)
        {
            this.incomingHeaders = incomingHeaders;
        }

        public HeaderDictionary(HeaderDictionary dictionary)
        {
            this.incomingHeaders = dictionary.GetDictionary();
        }

        IDictionary<string, string> GetDictionary()
        {
            return incomingHeaders;
        }

        IDictionary<string, string> GetOrCreateModifiable()
        {
            return incomingHeaders;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => GetDictionary().GetEnumerator();

        public void Add(KeyValuePair<string, string> item) => GetOrCreateModifiable().Add(item);

        public void Clear() => GetOrCreateModifiable().Clear();

        public bool Contains(KeyValuePair<string, string> item) => GetDictionary().Contains(item); //TODO switches to linq as IReadOnly doesn't have a contains?

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => GetOrCreateModifiable().CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, string> item) => GetOrCreateModifiable().Remove(item);

        public int Count => GetDictionary().Count;

        public bool IsReadOnly { get; } = false;

        public bool ContainsKey(string key) => GetDictionary().ContainsKey(key);

        public void Add(string key, string value) => GetOrCreateModifiable().Add(key, value);

        public bool Remove(string key) => GetOrCreateModifiable().Remove(key);

        public bool TryGetValue(string key, out string value)
        {
            var r = GetDictionary().TryGetValue(key, out string v);
            value = v;
            return r;
        }

        public string this[string key]
        {
            get => GetDictionary()[key];
            set => GetOrCreateModifiable()[key] = value;
        }

        public ICollection<string> Keys => GetOrCreateModifiable().Keys;
        public ICollection<string> Values => GetOrCreateModifiable().Values;
    }
}