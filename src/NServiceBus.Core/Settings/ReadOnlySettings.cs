namespace NServiceBus.Settings
{
    using ObjectBuilder;

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
        /// Gets the setting value.
        /// </summary>
        object Get(string key);

        T GetOrDefault<T>(string key);
        bool HasSetting(string key);
        bool HasSetting<T>();
        void ApplyTo<T>(IComponentConfig config);
    }
}