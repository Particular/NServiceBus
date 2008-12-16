using System;
using System.Collections.Generic;
using System.Text;
using ObjectBuilder;

namespace NServiceBus
{
    public static class ConfigureSerializers
    {
        public static Configure BinarySerializer(this Configure config)
        {
            config.Configurer.ConfigureComponent(typeof(NServiceBus.Serializers.Binary.MessageSerializer), ComponentCallModelEnum.Singleton);

            return config;
        }

        public static Configure XmlSerializer(this Configure config)
        {
            config.Configurer.ConfigureComponent<NServiceBus.MessageInterfaces.MessageMapper.Reflection.MessageMapper>(ComponentCallModelEnum.Singleton);
            config.Configurer.ConfigureComponent<NServiceBus.Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton);

            return config;
        }

        public static Configure XmlSerializer(this Configure config, string nameSpace)
        {
            config.Configurer.ConfigureComponent<NServiceBus.MessageInterfaces.MessageMapper.Reflection.MessageMapper>(ComponentCallModelEnum.Singleton);
            config.Configurer.ConfigureComponent<NServiceBus.Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                .Namespace = nameSpace;

            return config;
        }
    }
}
