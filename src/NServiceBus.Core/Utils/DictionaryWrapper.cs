namespace NServiceBus
{
    using System.Collections;
    using System.Collections.Generic;

    abstract class DictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>
    {
        IDictionary<TKey, TValue> inner;

        protected DictionaryWrapper(IDictionary<TKey, TValue> inner)
        {
            this.inner = inner;
        }

        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)inner).GetEnumerator();
        public virtual void Add(KeyValuePair<TKey, TValue> item) => inner.Add(item);
        public virtual void Clear() => inner.Clear();
        public virtual bool Contains(KeyValuePair<TKey, TValue> item) => inner.Contains(item);
        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => inner.CopyTo(array, arrayIndex);
        public virtual bool Remove(KeyValuePair<TKey, TValue> item) => inner.Remove(item);
        public virtual int Count => inner.Count;
        public virtual bool IsReadOnly => inner.IsReadOnly;
        public virtual bool ContainsKey(TKey key) => inner.ContainsKey(key);
        public virtual void Add(TKey key, TValue value) => inner.Add(key, value);
        public virtual bool Remove(TKey key) => inner.Remove(key);
        public virtual bool TryGetValue(TKey key, out TValue value) => inner.TryGetValue(key, out value);

        public virtual TValue this[TKey key]
        {
            get { return inner[key]; }
            set { inner[key] = value; }
        }

        public virtual ICollection<TKey> Keys => inner.Keys;
        public virtual ICollection<TValue> Values => inner.Values;
    }
}
