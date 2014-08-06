namespace NServiceBus.Serialization
{
    using System;
    using Utils.Reflection;

    class EnableSelectedSerializer : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            var selectedSerializerType = SelectedSerializerType(config);
            var serializationDefinition = selectedSerializerType.Construct<ISerializationDefinition>();

            config.Settings.Set<ISerializationDefinition>(serializationDefinition);

            config.EnableFeature(serializationDefinition.ProvidedByFeature);
        }

        static Type SelectedSerializerType(Configure config)
        {
            if (config.Settings.HasSetting("SelectedSerializer"))
            {
                return config.Settings.Get<Type>("SelectedSerializer");
            }
            return typeof(Xml);
        }
    }
}