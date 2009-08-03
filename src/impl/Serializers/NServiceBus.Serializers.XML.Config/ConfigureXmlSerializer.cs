using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureXmlSerializer
    {
        /// <summary>
        /// Use XML serialization that supports interface-based messages.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure XmlSerializer(this Configure config)
        {
            config.Configurer.ConfigureComponent<MessageInterfaces.MessageMapper.Reflection.MessageMapper>(ComponentCallModelEnum.Singleton);
            config.Configurer.ConfigureComponent<Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton);

            return config;
        }

        /// <summary>
        /// Use XML serialization that supports interface-based messages.
        /// Optionally set the namespace to be used in the XML.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="nameSpace"></param>
        /// <returns></returns>
        public static Configure XmlSerializer(this Configure config, string nameSpace)
        {
            config.Configurer.ConfigureComponent<MessageInterfaces.MessageMapper.Reflection.MessageMapper>(ComponentCallModelEnum.Singleton);
            config.Configurer.ConfigureComponent<Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(x => x.Namespace, nameSpace);

            return config;
        }

    }
}
