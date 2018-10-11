namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class RunSettings : IEnumerable<KeyValuePair<string, object>>
    {
        public TimeSpan? TestExecutionTimeout
        {
            get
            {
                TimeSpan? timeout;
                TryGet("TestExecutionTimeout", out timeout);
                return timeout;
            }
            set
            {
                Guard.AgainstNull(nameof(value), value);
                Set("TestExecutionTimeout", value);
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return stash.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Retrieves the specified type from the settings.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <returns>The type instance.</returns>
        public T Get<T>()
        {
            return Get<T>(typeof(T).FullName);
        }

        /// <summary>
        /// Retrieves the specified type from the settings
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="key">The key to retrieve the type.</param>
        /// <returns>The type instance.</returns>
        public T Get<T>(string key)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            T result;

            if (!TryGet(key, out result))
            {
                throw new KeyNotFoundException("No item found in behavior settings with key: " + key);
            }

            return result;
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
        /// Tries to retrieves the specified type from the settings.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="result">The type instance.</param>
        /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
        public bool TryGet<T>(out T result)
        {
            return TryGet(typeof(T).FullName, out result);
        }

        /// <summary>
        /// Stores the type instance in the settings.
        /// </summary>
        /// <typeparam name="T">The type to store.</typeparam>
        /// <param name="t">The instance type to store.</param>
        public void Set<T>(T t)
        {
            Set(typeof(T).FullName, t);
        }

        /// <summary>
        /// Removes the instance type from the settings.
        /// </summary>
        /// <typeparam name="T">The type to remove.</typeparam>
        public void Remove<T>()
        {
            Remove(typeof(T).FullName);
        }

        /// <summary>
        /// Removes the instance type from the settings.
        /// </summary>
        /// <param name="key">The key of the value being removed.</param>
        public void Remove(string key)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            stash.TryRemove(key, out _);
        }

        /// <summary>
        /// Stores the passed instance in the settings.
        /// </summary>
        public void Set<T>(string key, T t)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            stash[key] = t;
        }

        /// <summary>
        /// Tries to retrieves the specified type from the settings.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="key">The key of the value being looked up.</param>
        /// <param name="result">The type instance.</param>
        /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
        public bool TryGet<T>(string key, out T result)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            object value;
            if (stash.TryGetValue(key, out value))
            {
                result = (T) value;
                return true;
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
        /// Merges the passed settings into this one.
        /// </summary>
        /// <param name="settings">The source settings.</param>
        public void Merge(RunSettings settings)
        {
            foreach (var kvp in settings.stash)
            {
                stash[kvp.Key] = kvp.Value;
            }
        }

        ConcurrentDictionary<string, object> stash = new ConcurrentDictionary<string, object>();
    }
}