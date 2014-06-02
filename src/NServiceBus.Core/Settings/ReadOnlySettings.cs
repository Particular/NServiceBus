namespace NServiceBus.Settings
{
    using ObjectBuilder;

    /// <summary>
    /// Read only settings
    /// </summary>
    public interface ReadOnlySettings
    {
        /// <summary>
        /// Gets the setting value.
        /// </summary>
        /// <typeparam name="T">The value of the setting.</typeparam>
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
        /// Outputs the setting value to val, returning false if not found or type does not match
        /// </summary>
        /// <typeparam name="T">Expected type of the setting</typeparam>
        /// <param name="key">Key for the setting</param>
        /// <param name="val">Output parameter for the setting</param>
        /// <returns>True if found and type matches, false otherwise</returns>
        bool TryGetValue<T>(string key, out T val);

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