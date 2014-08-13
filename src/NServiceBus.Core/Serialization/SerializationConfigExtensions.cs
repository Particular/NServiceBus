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
        public static ConfigurationBuilder UseSerialization<T>(this ConfigurationBuilder config, Action<T> customizations = null) where T : SerializationDefinition
        {
            return UseSerialization(config, typeof(T), definition => {
                                                                         if (customizations != null)
                                                                         {
                                                                             customizations((T) definition);
                                                                         }
            });
        }

        /// <summary>
        /// Configures the given serializer to be used
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definitionType">The serializer definition eg <see cref="Json"/>, <see cref="Xml"/> etc</param>
        /// <param name="customizations">Any serializer customizations needed for the specified serializer</param>
        public static ConfigurationBuilder UseSerialization(this ConfigurationBuilder config, Type definitionType, Action<SerializationDefinition> customizations = null)
        {
            var definition = (SerializationDefinition)Activator.CreateInstance(definitionType, new object[]
            {
                config.settings
            });
            if (customizations != null)
            {
                customizations(definition);
            }

            config.settings.Set("SelectedSerializer", definition);

            return config;
        }


        internal static SerializationDefinition GetSelectedSerializer(this ReadOnlySettings settings)
        {
            SerializationDefinition selectedSerializer;
            if (settings.TryGet("SelectedSerializer", out selectedSerializer))
            {
                return selectedSerializer;
            }
            return new Xml(null);
        }
    }
}