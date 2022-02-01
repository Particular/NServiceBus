namespace NServiceBus.Extensibility
{
    using System.Collections.Generic;
    using Pipeline;

    /// <summary>
    /// A string object bag of context objects.
    /// </summary>
    public class ContextBag : IReadOnlyContextBag
    {
        /// <summary>
        /// Initialized a new instance of <see cref="ContextBag" />.
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
        /// <param name="key">The key of the value being looked up.</param>
        /// <param name="result">The type instance.</param>
        /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
        public bool TryGet<T>(string key, out T result)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            if (stash.TryGetValue(key, out var value))
            {
                result = (T)value;
                return true;
            }

            if (parentBag != null)
            {
                return parentBag.TryGet(key, out result);
            }

            result = default;
            return false;
        }

        /// <inheritdoc />
        public T Get<T>(string key)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);

            if (!TryGet(key, out T result))
            {
                throw new KeyNotFoundException("No item found in behavior context with key: " + key);
            }

            return result;
        }

        /// <summary>
        /// Gets the requested extension, a new one will be created if needed.
        /// </summary>
        public T GetOrCreate<T>() where T : class, new()
        {
            if (TryGet(out T value))
            {
                return value;
            }

            var newInstance = new T();

            Set(newInstance);

            return newInstance;
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
        /// Removes the instance type from the context.
        /// </summary>
        /// <param name="key">The key of the value being removed.</param>
        public void Remove(string key)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            stash.Remove(key);
        }

        /// <summary>
        /// Stores the passed instance in the context.
        /// </summary>
        public void Set<T>(string key, T t)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            stash[key] = t;
        }

        /// <summary>
        /// Merges the passed context into this one.
        /// </summary>
        /// <param name="context">The source context.</param>
        internal void Merge(ContextBag context)
        {
            foreach (var kvp in context.stash)
            {
                stash[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// This internal property is here for performance optimizations. It allows the pipeline to set all
        /// behaviors of a given stage which then can be extracted as part of the next delegate invocation from the context
        /// to avoid closure capturing. The behaviors set on the current bag take precedence. In case they are not set
        /// it walks up the parent hierarchy to find the appropriate parent behaviors (for stages that have no pipeline)
        /// </summary>
        internal IBehavior[] Behaviors
        {
            get => behaviors ?? parentBag?.Behaviors;
            set => behaviors = value;
        }

        ContextBag parentBag;
        IBehavior[] behaviors;

        Dictionary<string, object> stash = new Dictionary<string, object>();
    }
}