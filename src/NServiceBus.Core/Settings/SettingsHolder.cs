namespace NServiceBus.Settings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// Setting container.
    /// </summary>
    public static class SettingsHolder
    {
        static readonly ConcurrentDictionary<string, object> Overrides = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        static readonly ConcurrentDictionary<string, object> Defaults = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
  
        /// <summary>
        /// Gets the setting value.
        /// </summary>
        /// <typeparam name="T">The value of the setting.</typeparam>
        /// <param name="key">The key of the setting to get.</param>
        /// <returns>The setting value.</returns>
        public static T Get<T>(string key)
        {
            object result;
            if (Overrides.TryGetValue(key, out result))
            {
                return (T) result;
            }

            if (Defaults.TryGetValue(key, out result))
            {
                return (T)result;
            }

            throw new KeyNotFoundException(String.Format("The given key ({0}) was not present in the dictionary.", key));
        }

        /// <summary>
        /// Sets the setting value.
        /// </summary>
        /// <param name="key">The key to use to store the setting.</param>
        /// <param name="value">The setting value.</param>
        public static void Set(string key, object value)
        {
            Overrides[key] = value;
        }

        /// <summary>
        /// Sets the default setting value.
        /// </summary>
        /// <param name="key">The key to use to store the setting.</param>
        /// <param name="value">The setting value.</param>
        public static void SetDefault(string key, object value)
        {
            Defaults[key] = value;
        }
    }
}