namespace NServiceBus
{
    using System;
    using NServiceBus.Settings;
    using Serialization;

    /// <summary>
    /// Provides configuration options for serialization
    /// </summary>
    public static class SerializationConfigExtensions
    {
        /// <summary>
        /// Configures the given serializer to be used
        /// </summary>
        /// <typeparam name="T">The serializer definition eg <see cref="JsonSerializer"/>, <see cref="XmlSerializer"/>, etc</typeparam>
        /// <param name="config"></param>
        public static SerializationExtentions<T> UseSerialization<T>(this BusConfiguration config) where T : SerializationDefinition
        {
            Guard.AgainstNull(config, "config");
            var type = typeof(SerializationExtentions<>).MakeGenericType(typeof(T));
            var extension = (SerializationExtentions<T>)Activator.CreateInstance(type, config.Settings);
            var definition = (SerializationDefinition)Activator.CreateInstance(typeof(T));

            config.Settings.Set("SelectedSerializer", definition);

            return extension;
        }

        /// <summary>
        /// Configures the given serializer to be used
        /// </summary>
        /// <param name="config"></param>
        /// <param name="serializerType">The custom serializer type to use for serialization that implements <see cref="IMessageSerializer"/> or a derived type from <see cref="SerializationDefinition"/>.</param>
        public static void UseSerialization(this BusConfiguration config, Type serializerType)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNull(serializerType, "serializerType");

            if (typeof(SerializationDefinition).IsAssignableFrom(serializerType))
            {
                var definition = (SerializationDefinition)Activator.CreateInstance(serializerType);
                config.Settings.Set("SelectedSerializer", definition);
                return;
            }

            if (!typeof(IMessageSerializer).IsAssignableFrom(serializerType))
            {
                throw new ArgumentException("The type needs to implement IMessageSerializer.", "serializerType");
            }

            config.Settings.Set("SelectedSerializer", new CustomSerializer());
            config.Settings.Set("CustomSerializerType", serializerType);
        }

        internal static SerializationDefinition GetSelectedSerializer(this ReadOnlySettings settings)
        {
            SerializationDefinition selectedSerializer;
            if (settings.TryGet("SelectedSerializer", out selectedSerializer))
            {
                return selectedSerializer;
            }

            return new XmlSerializer();
        }
    }
}
