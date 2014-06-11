namespace NServiceBus
{
    using System;
    using Serialization;

    public static class SerializationConfigExtensions
    {
        public static Configure UseSerialization<T>(this Configure config, Action<SerializationConfiguration> customizations = null) where T : ISerializationDefinition
        {
            return UseSerialization(config, typeof(T), customizations);
        }

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