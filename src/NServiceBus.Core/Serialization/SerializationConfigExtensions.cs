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
            var type = typeof(SerializationExtentions<>).MakeGenericType(typeof(T));
            var extension = (SerializationExtentions<T>)Activator.CreateInstance(type, config.Settings);

            config.UseSerialization(typeof(T));

            return extension;
        }

        /// <summary>
        /// Configures the given serializer to be used
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definitionType">The serializer definition eg <see cref="JsonSerializer"/>, <see cref="XmlSerializer"/> etc</param>
        public static void UseSerialization(this BusConfiguration config, Type definitionType)
        {
            var definition = (SerializationDefinition)Activator.CreateInstance(definitionType);

            config.Settings.Set("SelectedSerializer", definition);
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
