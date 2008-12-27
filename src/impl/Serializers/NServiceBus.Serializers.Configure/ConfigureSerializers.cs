using System;
using System.Collections.Generic;
using System.Text;
using ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureSerializers
    {
        /// <summary>
        /// Use binary serialization.
        /// Note that this does not support interface-based messages.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure BinarySerializer(this Configure config)
        {
            config.Configurer.ConfigureComponent(typeof(NServiceBus.Serializers.Binary.MessageSerializer), ComponentCallModelEnum.Singleton);

            return config;
        }

        /// <summary>
        /// Use XML serialization that supports interface-based messages.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure XmlSerializer(this Configure config)
        {
            config.Configurer.ConfigureComponent<NServiceBus.MessageInterfaces.MessageMapper.Reflection.MessageMapper>(ComponentCallModelEnum.Singleton);
            config.Configurer.ConfigureComponent<NServiceBus.Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton);

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
            config.Configurer.ConfigureComponent<NServiceBus.MessageInterfaces.MessageMapper.Reflection.MessageMapper>(ComponentCallModelEnum.Singleton);
            config.Configurer.ConfigureComponent<NServiceBus.Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                .Namespace = nameSpace;

            return config;
        }
    }
}
