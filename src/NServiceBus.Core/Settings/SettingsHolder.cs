namespace NServiceBus.Settings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq.Expressions;
    using Utils.Reflection;

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
            return (T) Get(key);
        }

        /// <summary>
        /// Gets the setting value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object Get(string key)
        {
            object result;
            if (Overrides.TryGetValue(key, out result))
            {
                return result;
            }

            if (Defaults.TryGetValue(key, out result))
            {
                return result;
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
            EnsureWriteEnabled();

            Overrides[key] = value;
        }
        /// <summary>
        /// Sets the value
        /// </summary>
        /// <param name="key">The key to use to store the setting.</param>
        /// <param name="value">The setting value.</param>
        public static void Set<T>(object value)
        {
            Set(typeof(T).FullName, value);
        }
        public static void Set<T>(Action value)
        {
            Set(typeof(T).FullName, value);
        }

        /// <summary>
        /// Sets the value of the given property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public static void SetProperty<T>(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);

            Set(typeof(T).FullName + "." + prop.Name, value);
        }


        /// <summary>
        /// Sets the default value of the given property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public static void SetPropertyDefault<T>(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);

            SetDefault(typeof(T).FullName + "." + prop.Name, value);
        }


        /// <summary>
        /// Sets the default setting value.
        /// </summary>
        /// <param name="key">The key to use to store the setting.</param>
        /// <param name="value">The setting value.</param>
        public static void SetDefault<T>(object value)
        {
            SetDefault(typeof(T).FullName, value);
        }
        public static void SetDefault<T>(Action value)
        {
            SetDefault(typeof(T).FullName, value);
        }
        public static void SetDefault(string key, object value)
        {
            EnsureWriteEnabled();

            Defaults[key] = value;
        }

        public static void Reset()
        {
            locked = false;

            Overrides.Clear();
            Defaults.Clear();
        }

        public static T GetOrDefault<T>(string key)
        {
            object result;
            if (Overrides.TryGetValue(key, out result))
            {
                return (T)result;
            }

            if (Defaults.TryGetValue(key, out result))
            {
                return (T)result;
            }

            return default(T);
        }

        public static bool HasSetting(string key)
        {

            if (Overrides.ContainsKey(key))
            {
                return true;
            }

            if (Defaults.ContainsKey(key))
            {
                return true;
            }

            return false;
        }

        public static bool HasSetting<T>()
        {
            var key = typeof(T).FullName;

            return HasSetting(key);
        }

        /// <summary>
        /// Locks the settings to prevent further modifications
        /// </summary>
        public static void PreventChanges()
        {
            locked = true;
        }

        static void EnsureWriteEnabled()
        {
            if (locked)
            {
                throw new ConfigurationErrorsException(string.Format("The settings has been locked for modifications. Please move any configuration code earlier in the configuration pipeline"));
            }
        }

        static bool locked;

        public static void ApplyTo<T>()
        {
            var targetType = typeof (T);

            foreach (var property in targetType.GetProperties())
            {
                var settingsKey = targetType.FullName + "." + property.Name; 
                
                if (HasSetting(settingsKey))
                {
                    Configure.Instance.Configurer.ConfigureProperty<T>(property.Name, Get(settingsKey));
                }
            }
        }
    }
}