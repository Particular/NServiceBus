namespace NServiceBus.Extensibility
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A string object bag of context objects.
    /// </summary>
    public class ContextBag : ReadOnlyContextBag
    {
        /// <summary>
        /// Initialized a new instance of <see cref="ContextBag"/>.
        /// </summary>
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
        /// Gets the requested extension, a new one will be created if needed.
        /// </summary>
        public T GetOrCreate<T>() where T : class, new()
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
        /// Tries to retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="result">The type instance.</param>
        /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
        public bool TryRemove<T>(out T result)
        {
            var success = TryGet(typeof(T).FullName, out result);
            Remove<T>();
            return success;
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
        /// Merges the passed context into this one.
        /// </summary>
        /// <param name="context">The source context.</param>
        public void Merge(ContextBag context)
        {
            foreach (var kvp in context.stash)
            {
                stash[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Gets all items assignable to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The required base class or interface.</typeparam>
        public IEnumerable<T> GetAll<T>()
        {
            var parentItems = parentBag?.GetAll<T>() ?? Enumerable.Empty<T>();
            return stash.Values.OfType<T>().Concat(parentItems);
        }

        void Set<T>(string key, T t)
        {
            Guard.AgainstNullAndEmpty("key", key);
            stash[key] = t;
        }

        /// <summary>
        /// Walk the tree of context until one is found of the type <typeparamref name="T"/>.
        /// </summary>
        public bool TryGetRootContext<T>(out T result) where T : ContextBag
        {
            var cast = this as T;
            if (cast != null)
            {
                result = cast;
                return true;
            }

            if (parentBag == null)
            {
                result = null;
                return false;
            }

            return parentBag.TryGetRootContext(out result);
        }
        bool TryGet<T>(string key, out T result)
        {
            Guard.AgainstNullAndEmpty("key", key);
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
            Guard.AgainstNullAndEmpty("key", key);
            T result;

            if (!TryGet(key, out result))
            {
                throw new KeyNotFoundException("No item found in behavior context with key: " + key);
            }

            return result;
        }

        void Remove(string key)
        {
            Guard.AgainstNullAndEmpty("key", key);
            stash.Remove(key);
        }

        Dictionary<string, object> stash = new Dictionary<string, object>();
        ContextBag parentBag;
    }
}