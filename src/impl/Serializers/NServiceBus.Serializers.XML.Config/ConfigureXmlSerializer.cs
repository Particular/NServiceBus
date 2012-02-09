using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using System.Linq;
using NServiceBus.Serialization;

namespace NServiceBus
{
    using System;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.XML;

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
            if (!Configure.BuilderIsConfigured())
            {
                SetXmlSerializerAsDefault.UseXmlSerializer = true;
                return config;
            }

            var messageTypes = Configure.TypesToScan.Where(t => t.IsMessageType()).ToList();

            var mapper = new MessageMapper();
            mapper.Initialize(messageTypes);

            config.Configurer.RegisterSingleton<IMessageMapper>(mapper);
            config.Configurer.RegisterSingleton<IMessageCreator>(mapper);//todo - Modify the builders to auto register all types

            var serializer = new XmlMessageSerializer(mapper);
            serializer.Initialize(messageTypes);

            config.Configurer.RegisterSingleton<IMessageSerializer>(serializer);

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
            config.XmlSerializer();

            config.Configurer.ConfigureProperty<Serializers.XML.XmlMessageSerializer>(x => x.Namespace, nameSpace);

            return config;
        }
    }

    class SetXmlSerializerAsDefault : INeedInitialization
    {
        internal static bool UseXmlSerializer;

        void INeedInitialization.Init()
        {
            if (!Configure.Instance.Configurer.HasComponent<IMessageSerializer>() || UseXmlSerializer)
                Configure.Instance.XmlSerializer();
        }
    }
}
