namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Settings;
    using Serialization;

    /// <summary>
    /// Provides configuration options for serialization.
    /// </summary>
    public static class SerializationConfigExtensions
    {
        /// <summary>
        /// Configures the given serializer to be used.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="JsonSerializer"/>, <see cref="XmlSerializer"/>, etc.</typeparam>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static SerializationExtentions<T> UseSerialization<T>(this BusConfiguration config) where T : SerializationDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);
            var type = typeof(SerializationExtentions<>).MakeGenericType(typeof(T));
            var extension = (SerializationExtentions<T>)Activator.CreateInstance(type, config.Settings);
            var definition = (SerializationDefinition)Activator.CreateInstance(typeof(T));

            config.Settings.Set("SelectedSerializer", definition);

            return extension;
        }

        /// <summary>
        /// Configures the given serializer to be used.
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        /// <param name="serializerType">The custom serializer type to use for serialization that implements <see cref="IMessageSerializer"/> or a derived type from <see cref="SerializationDefinition"/>.</param>
        public static void UseSerialization(this BusConfiguration config, Type serializerType)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(serializerType), serializerType);

            if (typeof(SerializationDefinition).IsAssignableFrom(serializerType))
            {
                var definition = (SerializationDefinition)Activator.CreateInstance(serializerType);
                config.Settings.Set("SelectedSerializer", definition);
                return;
            }

            if (!typeof(IMessageSerializer).IsAssignableFrom(serializerType))
            {
                throw new ArgumentException("The type needs to implement IMessageSerializer.", nameof(serializerType));
            }

            config.Settings.Set("SelectedSerializer", new CustomSerializer());
            config.Settings.Set("CustomSerializerType", serializerType);
        }

        /// <summary>
        /// Configures additional deserializers to be considered when processing messages. Can be called multiple times.
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="JsonSerializer"/>, <see cref="XmlSerializer"/>, etc.</typeparam>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static void AddDeserializer<T>(this BusConfiguration config) where T : SerializationDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);

            Dictionary<RuntimeTypeHandle, SerializationDefinition> deserializers;
            var instance = Activator.CreateInstance<T>();
            var typeHandle = instance.ProvidedByFeature().TypeHandle;
            if (config.Settings.TryGet("AdditionalDeserializers", out deserializers))
            {
                deserializers[typeHandle] = instance;
            }
            else
            {
                deserializers = new Dictionary<RuntimeTypeHandle, SerializationDefinition>
                {
                    {typeHandle, instance}
                };
                config.Settings.Set("AdditionalDeserializers", deserializers);
            }
        }

        internal static SerializationDefinition GetSelectedSerializer(this ReadOnlySettings settings)
        {
            SerializationDefinition selectedSerializer;
            if (settings.TryGet("SelectedSerializer", out selectedSerializer))
            {
                return selectedSerializer;
            }

            return settings.Get<SerializationDefinition>("DefaultSerializer");
        }
    }
}
