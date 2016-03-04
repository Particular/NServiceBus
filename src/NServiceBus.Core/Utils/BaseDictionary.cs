namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(PREFIX + "DictionaryDebugView`2" + SUFFIX)]
    abstract class BaseDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public abstract int Count { get; }
        public abstract void Clear();
        public abstract void Add(TKey key, TValue value);
        public abstract bool ContainsKey(TKey key);
        public abstract bool Remove(TKey key);
        public abstract bool TryGetValue(TKey key, out TValue value);
        public abstract IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

        public bool IsReadOnly => false;

        public ICollection<TKey> Keys
        {
            get
            {
                if (keys == null)
                {
                    keys = new KeyCollection(this);
                }

                return keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (values == null)
                {
                    values = new ValueCollection(this);
                }

                return values;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                {
                    throw new KeyNotFoundException();
                }

                return value;
            }
            set { SetValue(key, value); }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue value;
            if (!TryGetValue(item.Key, out value))
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Copy(this, array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!Contains(item))
            {
                return false;
            }

            return Remove(item.Key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected abstract void SetValue(TKey key, TValue value);

        static void Copy<T>(ICollection<T> source, T[] array, int arrayIndex)
        {
            Guard.AgainstNull(nameof(array), array);

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < source.Count)
            {
                throw new ArgumentException("Destination array is not large enough. Check array.Length and arrayIndex.");
            }

            foreach (var item in source)
            {
                array[arrayIndex++] = item;
            }
        }

        KeyCollection keys;
        ValueCollection values;
        const string PREFIX = "System.Collections.Generic.Mscorlib_";
        const string SUFFIX = ",mscorlib,Version=2.0.0.0,Culture=neutral,PublicKeyToken=b77a5c561934e089";

        abstract class Collection<T> : ICollection<T>
        {
            protected Collection(IDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
            }

            public int Count => dictionary.Count;

            public bool IsReadOnly => true;

            public void CopyTo(T[] array, int arrayIndex)
            {
                Copy(this, array, arrayIndex);
            }

            public virtual bool Contains(T item)
            {
                foreach (var element in this)
                {
                    if (EqualityComparer<T>.Default.Equals(element, item))
                    {
                        return true;
                    }
                }
                return false;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return dictionary.Select(GetItem).GetEnumerator();
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException("Collection is read-only.");
            }

            public void Add(T item)
            {
                throw new NotSupportedException("Collection is read-only.");
            }

            public void Clear()
            {
                throw new NotSupportedException("Collection is read-only.");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            protected abstract T GetItem(KeyValuePair<TKey, TValue> pair);
            protected readonly IDictionary<TKey, TValue> dictionary;
        }

        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(PREFIX + "DictionaryKeyCollectionDebugView`2" + SUFFIX)]
        class KeyCollection : Collection<TKey>
        {
            public KeyCollection(IDictionary<TKey, TValue> dictionary)
                : base(dictionary)
            {
            }

            protected override TKey GetItem(KeyValuePair<TKey, TValue> pair)
            {
                return pair.Key;
            }

            public override bool Contains(TKey item)
            {
                return dictionary.ContainsKey(item);
            }
        }

        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(PREFIX + "DictionaryValueCollectionDebugView`2" + SUFFIX)]
        class ValueCollection : Collection<TValue>
        {
            public ValueCollection(IDictionary<TKey, TValue> dictionary)
                : base(dictionary)
            {
            }

            protected override TValue GetItem(KeyValuePair<TKey, TValue> pair)
            {
                return pair.Value;
            }
        }
    }
}