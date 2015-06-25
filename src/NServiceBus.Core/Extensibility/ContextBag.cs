namespace NServiceBus.Extensibility
{
    using System.Collections.Generic;

    /// <summary>
    /// A string object bag of context objects
    /// </summary>
    public class ContextBag
    {
        /// <summary>
        /// Initialized the bag
        /// </summary>
        /// <param name="parentBag"></param>
        public ContextBag(ContextBag parentBag = null)
        {
            this.parentBag = parentBag;
        }
        /// <summary>
        /// Retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <returns>The type instance.</returns>
        public T Get<T>()
        {
            return Get<T>(typeof(T).FullName);
        }


        /// <summary>
        /// Gets the requested extension, a new one will be created if needed
        /// </summary>
        public T GetOrCreate<T>() where T : class,new()
        {
            T value;

            if (TryGet(out value))
            {
                return value;
            }

            var newInstance = new T();

            Set(newInstance);

            return newInstance;
        }

        /// <summary>
        /// Tries to retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="result">The type instance.</param>
        /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
        public bool TryGet<T>(out T result)
        {
            return TryGet(typeof(T).FullName, out result);
        }


        /// <summary>
        /// Stores the type instance in the context.
        /// </summary>
        /// <typeparam name="T">The type to store.</typeparam>
        /// <param name="t">The instance type to store.</param>
        public void Set<T>(T t)
        {
            Set(typeof(T).FullName, t);
        }

        
        /// <summary>
        /// Removes the instance type from the context.
        /// </summary>
        /// <typeparam name="T">The type to remove.</typeparam>
        public void Remove<T>()
        {
            Remove(typeof(T).FullName);
        }

        /// <summary>
        /// Merges the passed context into this one
        /// </summary>
        /// <param name="context">The source context</param>
        public void Merge(ContextBag context)
        {
            foreach (var kvp in context.stash)
            {
                stash[kvp.Key] = kvp.Value;
            }
        }

        void Set<T>(string key, T t)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            stash[key] = t;
        }

        bool TryGet<T>(string key, out T result)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            object value;
            if (stash.TryGetValue(key, out value))
            {
                result = (T)value;
                return true;
            }

            if (parentBag != null)
            {
                return parentBag.TryGet(key, out result);
            }

            if (typeof(T).IsValueType)
            {
                result = default(T);
                return false;
            }

            result = default(T);
            return false;
        }

        T Get<T>(string key)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            T result;

            if (!TryGet(key, out result))
            {
                throw new KeyNotFoundException("No item found in behavior context with key: " + key);
            }

            return result;
        }

        void Remove(string key)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            stash.Remove(key);
        }

        Dictionary<string, object> stash = new Dictionary<string, object>();
        ContextBag parentBag;
    }
}