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
        /// <typeparam name="T">The serializer definition eg <see cref="Json"/>, <see cref="Xml"/> etc</typeparam>
        /// <param name="config"></param>
        /// <param name="customizations">Any serializer customizations needed for the specified serializer</param>
        public static void UseSerialization<T>(this ConfigurationBuilder config, Action<SerializationConfiguration> customizations = null) where T : ISerializationDefinition
        {
            UseSerialization(config, typeof(T), customizations);
        }

        /// <summary>
        /// Configures the given serializer to be used
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definitionType">The serializer definition eg J<see cref="Json"/>, <see cref="Xml"/> etc</param>
        /// <param name="customizations">Any serializer customizations needed for the specified serializer</param>
        public static void UseSerialization(this ConfigurationBuilder config, Type definitionType, Action<SerializationConfiguration> customizations = null)
        {
            if (customizations != null)
            {
                customizations(new SerializationConfiguration(config.settings));
            }

            config.settings.Set("SelectedSerializer", definitionType);
        }

        internal static Type GetSelectedSerializerType(this ReadOnlySettings settings)
        {
            Type selectedSerializer;
            if (settings.TryGet("SelectedSerializer", out selectedSerializer))
            {
                return selectedSerializer;
            }
            return typeof(Xml);
        }
    }
}