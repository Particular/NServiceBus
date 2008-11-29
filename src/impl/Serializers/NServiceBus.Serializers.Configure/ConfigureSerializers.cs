using System;
using System.Collections.Generic;
using System.Text;
using ObjectBuilder;

namespace NServiceBus.Config
{
    public static class ConfigureSerializers
    {
        public static Configure BinarySerializer(this Configure config)
        {
            config.builder.ConfigureComponent(typeof(NServiceBus.Serializers.Binary.MessageSerializer), ComponentCallModelEnum.Singleton);

            return config;
        }

        public static Configure XmlSerializer(this Configure config)
        {
            config.builder.ConfigureComponent<NServiceBus.Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton);

            return config;
        }

        public static Configure XmlSerializer(this Configure config, string nameSpace)
        {
            config.builder.ConfigureComponent<NServiceBus.Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                .Namespace = nameSpace;

            return config;
        }

        public static Configure InterfaceToXMLSerializer(this Configure config)
        {
            config.builder.ConfigureComponent<NServiceBus.Serializers.InterfacesToXML.MessageSerializer>(ComponentCallModelEnum.Singleton);

            return config;
        }

        public static Configure InterfaceToXMLSerializer(this Configure config, string nameSpace)
        {
            config.builder.ConfigureComponent<NServiceBus.Serializers.InterfacesToXML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                .Namespace = nameSpace;

            return config;
        }
    }
}
