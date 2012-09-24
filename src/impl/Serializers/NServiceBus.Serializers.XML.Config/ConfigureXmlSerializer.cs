using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    using System;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.XML;
    using Serializers.XML.Config;

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

            config.Configurer.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<XmlMessageSerializer>(DependencyLifecycle.SingleInstance);

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

            config.Configurer.ConfigureProperty<XmlMessageSerializer>(x => x.Namespace, nameSpace);

            return config;
        }

        /// <summary>
        /// Use XML serialization that supports interface-based messages.
        /// Optionally set the namespace to be used in the XML.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sanitizeInput"></param>
        /// <returns></returns>
        public static Configure XmlSerializer(this Configure config, bool sanitizeInput)
        {
          config.XmlSerializer();

          config.Configurer.ConfigureProperty<XmlMessageSerializer>(x => x.SanitizeInput, sanitizeInput);

          return config;
        }

        /// <summary>
        /// Use XML serialization that supports interface-based messages.
        /// Optionally set the namespace to be used in the XML.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="nameSpace"></param>
        /// <param name="sanitizeInput"></param>
        /// <returns></returns>
        public static Configure XmlSerializer(this Configure config, string nameSpace, bool sanitizeInput)
        {
          config.XmlSerializer();

          config.Configurer.ConfigureProperty<XmlMessageSerializer>(x => x.Namespace, nameSpace);
          config.Configurer.ConfigureProperty<XmlMessageSerializer>(x => x.SanitizeInput, sanitizeInput);

          return config;
        }
    }
}
