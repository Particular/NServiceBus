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
        /// <typeparam name="T">The value of the setting.</typeparam>
        /// <param name="key">The key of the setting to get.</param>
        /// <returns>The setting value.</returns>
        T Get<T>(string key);

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
        
        void ApplyTo<T>(IComponentConfig config);
    }
}
