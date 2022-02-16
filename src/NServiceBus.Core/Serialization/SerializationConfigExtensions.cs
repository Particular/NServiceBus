namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Serialization;
    using Settings;

    /// <summary>
    /// Provides configuration options for serialization.
    /// </summary>
    public static class SerializationConfigExtensions
    {
        const string DisableDynamicTypeLoadingKey = "NServiceBus.Serialization.DisableDynamicTypeLoading";

        /// <summary>
        /// Configures the given serializer to be used.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="XmlSerializer" />.</typeparam>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static SerializationExtensions<T> UseSerialization<T>(this EndpointConfiguration config) where T : SerializationDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);
            var definition = (T)Activator.CreateInstance(typeof(T));

            return UseSerialization(config, definition);
        }

        /// <summary>
        /// Configures the given serializer to be used.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="XmlSerializer" />.</typeparam>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="serializationDefinition">An instance of serialization definition.</param>
        public static SerializationExtensions<T> UseSerialization<T>(this EndpointConfiguration config, T serializationDefinition) where T : SerializationDefinition
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(serializationDefinition), serializationDefinition);

            var settings = new SettingsHolder();
            config.Settings.SetMainSerializer(serializationDefinition, settings);
            return CreateSerializationExtension<T>(settings, config.Settings);
        }

        /// <summary>
        /// Configures additional deserializers to be considered when processing messages. Can be called multiple times.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="XmlSerializer" />.</typeparam>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static SerializationExtensions<T> AddDeserializer<T>(this EndpointConfiguration config) where T : SerializationDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);
            var definition = (T)Activator.CreateInstance(typeof(T));

            return AddDeserializer(config, definition);
        }

        /// <summary>
        /// Configures additional deserializers to be considered when processing messages. Can be called multiple times.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="XmlSerializer" />.</typeparam>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="serializationDefinition">An instance of serialization definition.</param>
        public static SerializationExtensions<T> AddDeserializer<T>(this EndpointConfiguration config, T serializationDefinition) where T : SerializationDefinition
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(serializationDefinition), serializationDefinition);

            var additionalSerializers = config.GetSettings().GetAdditionalSerializers();

            var settings = new SettingsHolder();
            additionalSerializers.Add(Tuple.Create<SerializationDefinition, SettingsHolder>(serializationDefinition, settings));
            return CreateSerializationExtension<T>(settings, config.Settings);
        }

        /// <summary>
        /// Disables dynamic type loading via `Type.GetType` to prevent loading of assemblies for types passed in message header `NServiceBus.EnclosedMessageTypes` to only allow message types during deserialization that were explicitly loaded.  
        /// </summary>
        public static void DisableDynamicTypeLoading<T>(this SerializationExtensions<T> config) where T : SerializationDefinition
        {
            Guard.AgainstNull(nameof(config), config);
            config.EndpointConfigurationSettings.Set(DisableDynamicTypeLoadingKey, true);
        }

        internal static bool IsDynamicTypeLoadingEnabled(this IReadOnlySettings endpointConfigurationSettings)
        {
            return !endpointConfigurationSettings.GetOrDefault<bool>(DisableDynamicTypeLoadingKey);
        }

        static SerializationExtensions<T> CreateSerializationExtension<T>(SettingsHolder serializerSettings, SettingsHolder endpointConfigurationSettings) where T : SerializationDefinition => new SerializationExtensions<T>(serializerSettings, endpointConfigurationSettings);
    }
}
