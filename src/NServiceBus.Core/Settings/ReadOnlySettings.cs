namespace NServiceBus.Settings
{
    using ObjectBuilder;

    /// <summary>
    /// Settings for readonly.
    /// </summary>
    public interface ReadOnlySettings
    {
        /// <summary>
        /// Gets the setting value.
        /// </summary>
        /// <typeparam name="T">The <typeparamref name="T"/> to locate in the <see cref="ReadOnlySettings"/>.</typeparam>
        /// <returns>The setting value.</returns>
        T Get<T>();
        
        /// <summary>
        /// Gets the setting value.
        /// </summary>
        /// <typeparam name="T">The type of the setting.</typeparam>
        /// <param name="key">The key of the setting to get.</param>
        /// <returns>The setting value.</returns>
        T Get<T>(string key);

        /// <summary>
        /// Safely get the settings value, returning false if the settings key was not found.
        /// </summary>
        /// <typeparam name="T">The type to get, fullname will be used as key</typeparam>
        /// <param name="val">The value if present</param>
        bool TryGet<T>(out T val);

        /// <summary>
        /// Safely get the settings value, returning false if the settings key was not found.
        /// </summary>
        /// <typeparam name="T">The type of the setting.</typeparam>
        /// <param name="key">The key of the setting to get.</param>
        /// <param name="val">The setting value.</param>
        /// <returns>True if found, false otherwise</returns>
        bool TryGet<T>(string key, out T val);

        /// <summary>
        /// Gets the setting value.
        /// </summary>
        object Get(string key);

        /// <summary>
        /// Gets the setting value or the <code>default(T)</code>.
        /// </summary>
        /// <typeparam name="T">The value of the setting.</typeparam>
        /// <param name="key">The key of the setting to get.</param>
        /// <returns>The setting value.</returns>
        T GetOrDefault<T>(string key);
        
        /// <summary>
        /// Determines whether the <see cref="ReadOnlySettings"/> contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ReadOnlySettings"/></param>
        /// <returns>true if the <see cref="ReadOnlySettings"/> contains an element with the specified key; otherwise, false.</returns>
        bool HasSetting(string key);
        
        /// <summary>
        /// Determines whether the <see cref="ReadOnlySettings"/> contains the specified <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The <typeparamref name="T"/> to locate in the <see cref="ReadOnlySettings"/>.</typeparam>
        /// <returns>true if the <see cref="ReadOnlySettings"/> contains an element with the specified key; otherwise, false.</returns>
        bool HasSetting<T>();

        /// <summary>
        /// Determines whether the <see cref="ReadOnlySettings"/> contains a specific value for the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ReadOnlySettings"/></param>
        /// <returns>true if the <see cref="ReadOnlySettings"/> contains an explicit value with the specified key; otherwise, false.</returns>
        bool HasExplicitValue(string key);

        /// <summary>
        /// Determines whether the <see cref="ReadOnlySettings"/> contains a specific value for the specified <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The <typeparamref name="T"/> to locate in the <see cref="ReadOnlySettings"/>.</typeparam>
        /// <returns>true if the <see cref="ReadOnlySettings"/> contains an element with the specified key; otherwise, false.</returns>
        bool HasExplicitValue<T>();
        
        /// <summary>
        /// Setup property injection for the given type based on convention
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        void ApplyTo<T>(IComponentConfig config);
    }
}
