using System;

namespace NServiceBus.Utils
{
    using System.Collections.Generic;

    internal class WeakReference<T> : WeakReference where T : class
    {
        public static WeakReference<T> Create(T target)
        {
            if (target == null)
                return WeakNullReference<T>.Singleton;

            return new WeakReference<T>(target);
        }

        protected WeakReference(T target)
            : base(target, false) { }

        public new T Target
        {
            get { return (T)base.Target; }
        }
    }

    internal class WeakNullReference<T> : WeakReference<T> where T : class
    {
        public static readonly WeakNullReference<T> Singleton = new WeakNullReference<T>();

        private WeakNullReference()
            : base(null)
        {
        }

        public override bool IsAlive
        {
            get { return true; }
        }
    }

    internal sealed class WeakKeyReference<T> : WeakReference<T> where T : class
    {
        public readonly int HashCode;

        public WeakKeyReference(T key, WeakKeyComparer<T> comparer)
            : base(key)
        {
            // retain the object's hash code immediately so that even
            // if the target is GC'ed we will be able to find and
            // remove the dead weak reference.
            HashCode = comparer.GetHashCode(key);
        }
    }

    internal sealed class WeakKeyComparer<T> : IEqualityComparer<object>
    where T : class
    {
        private IEqualityComparer<T> comparer;

        internal WeakKeyComparer(IEqualityComparer<T> comparer)
        {
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            this.comparer = comparer;
        }

        public int GetHashCode(object obj)
        {
            var weakKey = obj as WeakKeyReference<T>;
            if (weakKey != null) return weakKey.HashCode;
            return comparer.GetHashCode((T)obj);
        }

        // Note: There are actually 9 cases to handle here.
        //
        //  Let Wa = Alive Weak Reference
        //  Let Wd = Dead Weak Reference
        //  Let S  = Strong Reference
        //
        //  x  | y  | Equals(x,y)
        // -------------------------------------------------
        //  Wa | Wa | comparer.Equals(x.Target, y.Target)
        //  Wa | Wd | false
        //  Wa | S  | comparer.Equals(x.Target, y)
        //  Wd | Wa | false
        //  Wd | Wd | x == y
        //  Wd | S  | false
        //  S  | Wa | comparer.Equals(x, y.Target)
        //  S  | Wd | false
        //  S  | S  | comparer.Equals(x, y)
        // -------------------------------------------------
        public new bool Equals(object x, object y)
        {
            bool xIsDead, yIsDead;
            var first = GetTarget(x, out xIsDead);
            var second = GetTarget(y, out yIsDead);

            if (xIsDead)
                return yIsDead && x == y;

            if (yIsDead)
                return false;

            return comparer.Equals(first, second);
        }

        private static T GetTarget(object obj, out bool isDead)
        {
            var wref = obj as WeakKeyReference<T>;
            T target;
            if (wref != null)
            {
                target = wref.Target;
                isDead = !wref.IsAlive;
            }
            else
            {
                target = (T)obj;
                isDead = false;
            }
            return target;
        }
    }

    internal sealed class WeakKeyDictionary<TKey, TValue> : BaseDictionary<TKey, TValue>
        where TKey : class
    {
        private Dictionary<object, TValue> dictionary;
        private WeakKeyComparer<TKey> comparer;

        public WeakKeyDictionary()
            : this(0, null) { }

        public WeakKeyDictionary(int capacity)
            : this(capacity, null) { }

        public WeakKeyDictionary(IEqualityComparer<TKey> comparer)
            : this(0, comparer) { }

        public WeakKeyDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.comparer = new WeakKeyComparer<TKey>(comparer);
            dictionary = new Dictionary<object, TValue>(capacity, this.comparer);
        }

        // WARNING: The count returned here may include entries for which
        // either the key or value objects have already been garbage
        // collected. Call RemoveCollectedEntries to weed out collected
        // entries and update the count accordingly.
        public override int Count
        {
            get { return dictionary.Count; }
        }

        public override void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException("key");
            WeakReference<TKey> weakKey = new WeakKeyReference<TKey>(key, comparer);
            dictionary.Add(weakKey, value);
        }

        public override bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public override bool Remove(TKey key)
        {
            return dictionary.Remove(key);
        }

        public override bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        protected override void SetValue(TKey key, TValue value)
        {
            WeakReference<TKey> weakKey = new WeakKeyReference<TKey>(key, comparer);
            dictionary[weakKey] = value;
        }

        public override void Clear()
        {
            dictionary.Clear();
        }

        public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var kvp in dictionary)
            {
                var weakKey = (WeakReference<TKey>)(kvp.Key);
                var key = weakKey.Target;
                var value = kvp.Value;
                if (weakKey.IsAlive)
                    yield return new KeyValuePair<TKey, TValue>(key, value);
            }
        }

        // Removes the left-over weak references for entries in the dictionary
        // whose key or value has already been reclaimed by the garbage
        // collector. This will reduce the dictionary's Count by the number
        // of dead key-value pairs that were eliminated.
        public void RemoveCollectedEntries()
        {
            List<object> toRemove = null;
            foreach (var pair in dictionary)
            {
                var weakKey = (WeakReference<TKey>)(pair.Key);

                if (!weakKey.IsAlive)
                {
                    if (toRemove == null)
                        toRemove = new List<object>();
                    toRemove.Add(weakKey);
                }
            }

            if (toRemove != null)
            {
                foreach (var key in toRemove)
                    dictionary.Remove(key);
            }
        }
    }
}