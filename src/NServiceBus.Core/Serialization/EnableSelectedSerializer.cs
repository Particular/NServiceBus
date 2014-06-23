namespace NServiceBus.Serialization
{
    using System;
    using Utils.Reflection;

    class EnableSelectedSerializer : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            if (!config.Settings.HasSetting("SelectedSerializer"))
            {
                config.UseSerialization<Xml>();
            }

            var serializationDefinition = config.Settings.Get<Type>("SelectedSerializer").Construct<ISerializationDefinition>();

            config.Settings.Set<ISerializationDefinition>(serializationDefinition);

            config.EnableFeature(serializationDefinition.ProvidedByFeature);
        }

    }
}