namespace NServiceBus.Settings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq.Expressions;
    using ObjectBuilder;
    using Utils.Reflection;
/// <summary>
    /// Setting container.
    /// </summary>
    public class SettingsHolder:ReadOnlySettings
    {
        public static SettingsHolder Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SettingsHolder();
                }
                return instance;
            }
        }

        static SettingsHolder instance;

        /// <summary>
        /// Gets the setting value.
        /// </summary>
        /// <typeparam name="T">The value of the setting.</typeparam>
        /// <param name="key">The key of the setting to get.</param>
        /// <returns>The setting value.</returns>
        public T Get<T>(string key)
        {
            return (T) Get(key);
        }

        /// <summary>
        /// Gets the setting value.
        /// </summary>
        /// <typeparam name="T">The value of the setting.</typeparam>
        /// <returns>The setting value.</returns>
        public T Get<T>()
        {
            return (T)Get(typeof(T).FullName);
        }

        /// <summary>
        /// Gets the setting value.
        /// </summary>
        public object Get(string key)
        {
            object result;
            if (Instance.Overrides.TryGetValue(key, out result))
            {
                return result;
            }

            if (Instance.Defaults.TryGetValue(key, out result))
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
        public void Set(string key, object value)
        {
            EnsureWriteEnabled(key);

            Instance.Overrides[key] = value;
        }
        /// <summary>
        /// Sets the value
        /// </summary>
        /// <typeparam name="T">The type to use as a key for storing the setting.</typeparam>
        /// <param name="value">The setting value.</param>
        public void Set<T>(object value)
        {
            Set(typeof(T).FullName, value);
        }
        public void Set<T>(Action value)
        {
            Set(typeof(T).FullName, value);
        }

        /// <summary>
        /// Sets the value of the given property
        /// </summary>
        public void SetProperty<T>(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);

            Set(typeof(T).FullName + "." + prop.Name, value);
        }

        /// <summary>
        /// Sets the default value of the given property
        /// </summary>
        public void SetPropertyDefault<T>(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);

            SetDefault(typeof(T).FullName + "." + prop.Name, value);
        }

        /// <summary>
        /// Sets the default setting value.
        /// </summary>
        /// <typeparam name="T">The type to use as a key for storing the setting.</typeparam>
        /// <param name="value">The setting value.</param>
        public void SetDefault<T>(object value)
        {
            SetDefault(typeof(T).FullName, value);
        }

        public void SetDefault<T>(Action value)
        {
            SetDefault(typeof(T).FullName, value);
        }

        public void SetDefault(string key, object value)
        {
            EnsureWriteEnabled(key);

            Instance.Defaults[key] = value;
        }

        public void Reset()
        {
            locked = false;

            Instance.Overrides.Clear();
            Instance.Defaults.Clear();
        }

        public T GetOrDefault<T>(string key)
        {
            object result;
            if (Instance.Overrides.TryGetValue(key, out result))
            {
                return (T)result;
            }

            if (Instance.Defaults.TryGetValue(key, out result))
            {
                return (T)result;
            }

            return default(T);
        }

        public bool HasSetting(string key)
        {

            if (Instance.Overrides.ContainsKey(key))
            {
                return true;
            }

            if (Instance.Defaults.ContainsKey(key))
            {
                return true;
            }

            return false;
        }

        public bool HasSetting<T>()
        {
            var key = typeof(T).FullName;

            return HasSetting(key);
        }

        /// <summary>
        /// Locks the settings to prevent further modifications
        /// </summary>
        public void PreventChanges()
        {
            locked = true;
        }

        void EnsureWriteEnabled(string key)
        {
            if (locked)
            {
                throw new ConfigurationErrorsException(string.Format("Unable to set the value for key: {0}. The settings has been locked for modifications. Please move any configuration code earlier in the configuration pipeline", key));
            }
        }

        bool locked;

        public void ApplyTo<T>(IComponentConfig config)
        {
            var targetType = typeof (T);

            foreach (var property in targetType.GetProperties())
            {
                var settingsKey = targetType.FullName + "." + property.Name; 
                
                if (HasSetting(settingsKey))
                {
                    config.ConfigureProperty(property.Name, Get(settingsKey));
                }
            }
        }

        readonly ConcurrentDictionary<string, object> Overrides = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        readonly ConcurrentDictionary<string, object> Defaults = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    }

    public interface ReadOnlySettings
    {
        /// <summary>
        /// Gets the setting value.
        /// </summary>
        /// <typeparam name="T">The value of the setting.</typeparam>
        /// <param name="key">The key of the setting to get.</param>
        /// <returns>The setting value.</returns>
        T Get<T>(string key);

        /// <summary>
        /// Gets the setting value.
        /// </summary>
        object Get(string key);

        T GetOrDefault<T>(string key);
        bool HasSetting(string key);
        bool HasSetting<T>();
        void ApplyTo<T>(IComponentConfig config);
    }
}