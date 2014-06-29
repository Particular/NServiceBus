namespace NServiceBus
{
    using System;
    using Serialization;

    /// <summary>
    /// Provides configuration options for serialization
    /// </summary>
    public static class SerializationConfigExtensions
    {
        /// <summary>
        /// Configures the given serializer to be used
        /// </summary>
        /// <typeparam name="T">The serializer definition eg JSON, XML etc</typeparam>
        /// <param name="config"></param>
        /// <param name="customizations">Any serializer customizations needed for the specified serializer</param>
        /// <returns></returns>
        public static Configure UseSerialization<T>(this Configure config, Action<SerializationConfiguration> customizations = null) where T : ISerializationDefinition
        {
            return UseSerialization(config, typeof(T), customizations);
        }

        /// <summary>
        /// Configures the given serializer to be used
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definitionType">The serializer definition eg JSON, XML etc</param>
        /// <param name="customizations">Any serializer customizations needed for the specified serializer</param>
        /// <returns></returns>
        public static Configure UseSerialization(this Configure config, Type definitionType, Action<SerializationConfiguration> customizations = null)
        {
            if (customizations != null)
            {
                customizations(new SerializationConfiguration(config.Settings));
            }

            config.Settings.Set("SelectedSerializer", definitionType);

            return config;
        }
    }
}