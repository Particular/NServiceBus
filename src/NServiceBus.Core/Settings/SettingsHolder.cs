namespace NServiceBus.Settings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;

    /// <summary>
    /// Setting container.
    /// </summary>
    public partial class SettingsHolder : ReadOnlySettings
    {
        /// <summary>
        /// Gets the given setting by key.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        public T Get<T>(string key)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            return (T) Get(key);
        }

        /// <summary>
        /// Tries to get the given value, key is the type fullname.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="val">The returned value if present.</param>
        /// <returns>True if found.</returns>
        public bool TryGet<T>(out T val)
        {
            return TryGet(typeof(T).FullName, out val);
        }

        /// <summary>
        /// Tries to get the given value by key.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="val">Value if found.</param>
        /// <returns>True if key is found.</returns>
        public bool TryGet<T>(string key, out T val)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            val = default(T);

            object tmp;
            if (!Overrides.TryGetValue(key, out tmp))
            {
                if (!Defaults.TryGetValue(key, out tmp))
                {
                    return false;
                }
            }

            if (!(tmp is T))
            {
                return false;
            }

            val = (T) tmp;
            return true;
        }

        /// <summary>
        /// Gets the given value, key is type fullname.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>The value if found, throws if not.</returns>
        public T Get<T>()
        {
            return (T) Get(typeof(T).FullName);
        }

        /// <summary>
        /// Gets the given value by key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        public object Get(string key)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            object result;
            if (Overrides.TryGetValue(key, out result))
            {
                return result;
            }

            if (Defaults.TryGetValue(key, out result))
            {
                return result;
            }

            throw new KeyNotFoundException($"The given key ({key}) was not present in the dictionary.");
        }

        /// <summary>
        /// Gets the setting or default based on the typename.
        /// </summary>
        /// <typeparam name="T">The setting to get.</typeparam>
        /// <returns>The actual value.</returns>
        public T GetOrDefault<T>()
        {
            return GetOrDefault<T>(typeof(T).FullName);
        }

        /// <summary>
        /// Gets the value or its default.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        public T GetOrDefault<T>(string key)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            object result;
            if (Overrides.TryGetValue(key, out result))
            {
                return (T) result;
            }

            if (Defaults.TryGetValue(key, out result))
            {
                return (T) result;
            }

            return default(T);
        }

        /// <summary>
        /// True if there is a default or explicit value for the given key.
        /// </summary>
        /// <param name="key">The Key.</param>
        /// <returns>True if found.</returns>
        public bool HasSetting(string key)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            return Overrides.ContainsKey(key) || Defaults.ContainsKey(key);
        }

        /// <summary>
        /// True if there is a setting for the given type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>True if found.</returns>
        public bool HasSetting<T>()
        {
            var key = typeof(T).FullName;

            return HasSetting(key);
        }

        /// <summary>
        /// True if there is an explicit value for the given key.
        /// </summary>
        /// <param name="key">The Key.</param>
        /// <returns>True if found.</returns>
        public bool HasExplicitValue(string key)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            return Overrides.ContainsKey(key);
        }

        /// <summary>
        /// True if there is an explicit value for the given type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>True if found.</returns>
        public bool HasExplicitValue<T>()
        {
            var key = typeof(T).FullName;

            return HasExplicitValue(key);
        }

        /// <summary>
        /// Gets the requested value, a new one will be created and added if needed.
        /// </summary>
        public T GetOrCreate<T>()
            where T : class, new()
        {
            T value;
            if (!TryGet(out value))
            {
                value = new T();
                Set<T>(value);
            }
            return value;
        }

        /// <summary>
        /// Sets the setting value.
        /// </summary>
        /// <param name="key">The key to use to store the setting.</param>
        /// <param name="value">The setting value.</param>
        public void Set(string key, object value)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            EnsureWriteEnabled(key);

            Overrides[key] = value;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <typeparam name="T">The type to use as a key for storing the setting.</typeparam>
        /// <param name="value">The setting value.</param>
        public void Set<T>(object value)
        {
            Set(typeof(T).FullName, value);
        }

        /// <summary>
        /// Sets the given value, key is type fullname.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="value">Action to store.</param>
        public void Set<T>(Action value)
        {
            Set(typeof(T).FullName, value);
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

        /// <summary>
        /// Sets the default value for the given setting.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="value">The value to store as default.</param>
        public void SetDefault<T>(Action value)
        {
            SetDefault(typeof(T).FullName, value);
        }

        /// <summary>
        /// Set the default value for the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetDefault(string key, object value)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            EnsureWriteEnabled(key);

            Defaults[key] = value;
        }

        /// <summary>
        /// Locks the settings to prevent further modifications.
        /// </summary>
        internal void PreventChanges()
        {
            locked = true;
        }

        internal void Merge(ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            EnsureMergingIsPossible();

            var holder = settings as SettingsHolder ?? new SettingsHolder();

            foreach (var @default in holder.Defaults)
            {
                Defaults[@default.Key] = @default.Value;
            }

            foreach (var @override in holder.Overrides)
            {
                Overrides[@override.Key] = @override.Value;
            }
        }

        void EnsureMergingIsPossible()
        {
            if (locked)
            {
                throw new ConfigurationErrorsException("Unable to merge settings. The settings has been locked for modifications. Move any configuration code earlier in the configuration pipeline");
            }
        }

        void EnsureWriteEnabled(string key)
        {
            if (locked)
            {
                throw new ConfigurationErrorsException($"Unable to set the value for key: {key}. The settings has been locked for modifications. Move any configuration code earlier in the configuration pipeline");
            }
        }

        /// <summary>
        /// Clears the settings holder default values and overrides, if a value is disposable the dispose method will be called.
        /// </summary>
        public void Clear()
        {
            foreach (var item in Defaults)
            {
                (item.Value as IDisposable)?.Dispose();
            }

            Defaults.Clear();

            foreach (var item in Overrides)
            {
                (item.Value as IDisposable)?.Dispose();
            }

            Overrides.Clear();
        }

        ConcurrentDictionary<string, object> Defaults = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        bool locked;

        ConcurrentDictionary<string, object> Overrides = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
}