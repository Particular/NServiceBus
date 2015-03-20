namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using ObjectBuilder;

    /// <summary>
    /// Base class for a pipeline behavior.
    /// </summary>
    public abstract class BehaviorContext
    {
        /// <summary>
        /// Create an instance of <see cref="BehaviorContext"/>.
        /// </summary>
        /// <param name="parentContext">The parent context</param>
        protected BehaviorContext(BehaviorContext parentContext)
        {
            this.parentContext = parentContext;
        }

        /// <summary>
        /// The current <see cref="IBuilder"/>
        /// </summary>
        public IBuilder Builder
        {
            get
            {
                var rawBuilder = Get<IBuilder>();
                return rawBuilder;
            }
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
        /// Tries to retrieves the specified type from the context using a custom key.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="key">The custom key.</param>
        /// <param name="result">The type instance.</param>
        /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
        public bool TryGet<T>(string key, out T result)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            object value;
            if (stash.TryGetValue(key, out value))
            {
                result = (T) value;
                return true;
            }

            if (parentContext != null)
            {
                return parentContext.TryGet(key, out result);
            }

            if (typeof(T).IsValueType)
            {
                result = default(T);
                return false;
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="key">The custom key.</param>
        /// <returns>The type instance.</returns>
        public T Get<T>(string key)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            T result;

            if (!TryGet(key, out result))
            {
                throw new KeyNotFoundException("No item found in behavior context with key: " + key);
            }

            return result;
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
        /// Stores the type instance in the context using a custom key.
        /// </summary>
        /// <typeparam name="T">The type to store.</typeparam>
        /// <param name="key">The custom key.</param>
        /// <param name="t">The instance type to store.</param>
        public void Set<T>(string key, T t)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            stash[key] = t;
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
        /// Removes a entry from the context using the specifed custom key.
        /// </summary>
        /// <param name="key">The custom key.</param>
        public void Remove(string key)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            stash.Remove(key);
        }

        /// <summary>
        /// Access to the parent context
        /// </summary>
        protected readonly BehaviorContext parentContext;

        internal bool handleCurrentMessageLaterWasCalled;

        Dictionary<string, object> stash = new Dictionary<string, object>();
    }
}