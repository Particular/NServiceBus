namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Serialization;
    using Settings;

    /// <summary>
    /// Provides configuration options for serialization.
    /// </summary>
    public static partial class SerializationConfigExtensions
    {
        /// <summary>
        /// Configures the given serializer to be used.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="JsonSerializer" />, <see cref="XmlSerializer" />, etc.</typeparam>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static SerializationExtensions<T> UseSerialization<T>(this EndpointConfiguration config) where T : SerializationDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);
            var definition = (T) Activator.CreateInstance(typeof(T));

            return UseSerialization(config, definition);
        }

        /// <summary>
        /// Configures the given serializer to be used.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="JsonSerializer" />, <see cref="XmlSerializer" />, etc.</typeparam>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="serializationDefinition">An instance of serialization definition.</param>
        public static SerializationExtensions<T> UseSerialization<T>(this EndpointConfiguration config, T serializationDefinition) where T : SerializationDefinition
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(serializationDefinition), serializationDefinition);

            var settings = new SettingsHolder();
            config.Settings.SetMainSerializer(serializationDefinition, settings);
            return CreateSerializationExtension<T>(settings);
        }

        /// <summary>
        /// Configures additional deserializers to be considered when processing messages. Can be called multiple times.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="JsonSerializer" />, <see cref="XmlSerializer" />, etc.</typeparam>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static SerializationExtensions<T> AddDeserializer<T>(this EndpointConfiguration config) where T : SerializationDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);
            var definition = (T) Activator.CreateInstance(typeof(T));

            return AddDeserializer(config, definition);
        }

        /// <summary>
        /// Configures additional deserializers to be considered when processing messages. Can be called multiple times.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="JsonSerializer" />, <see cref="XmlSerializer" />, etc.</typeparam>
        /// <param name="serializationDefinition">An instance of serialization definition.</param>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static SerializationExtensions<T> AddDeserializer<T>(this EndpointConfiguration config, T serializationDefinition) where T : SerializationDefinition
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(serializationDefinition), serializationDefinition);

            var additionalSerializers = config.GetSettings().GetAdditionalSerializers();

            var settings = new SettingsHolder();
            additionalSerializers.Add(Tuple.Create<SerializationDefinition, SettingsHolder>(serializationDefinition, settings));
            return CreateSerializationExtension<T>(settings);
        }

        static SerializationExtensions<T> CreateSerializationExtension<T>(SettingsHolder settings) where T : SerializationDefinition
        {
            var type = typeof(SerializationExtensions<>).MakeGenericType(typeof(T));
            var extension = (SerializationExtensions<T>) Activator.CreateInstance(type, settings);
            return extension;
        }
    }
}