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
        /// Optionally set the namespace to be used in the XML.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="nameSpace">The namespace to use</param>
        /// <param name="sanitizeInput">Sanatizes the xml input if set to true</param>
        /// <param name="dontWrapSingleMessages">Tells the serializer to not wrap single messages with a "messages" element. This will break compatibility with endpoints older thatn 3.4.0 </param>
        /// <returns></returns>
        public static Configure XmlSerializer(this Configure config, string nameSpace = null, bool sanitizeInput = false,bool dontWrapSingleMessages = false)
        {
            if (!Configure.BuilderIsConfigured())
            {
                SetXmlSerializerAsDefault.UseXmlSerializer = true;
                return config;
            }

            config.Configurer.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<XmlMessageSerializer>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureProperty<XmlMessageSerializer>(x => x.SanitizeInput, sanitizeInput);
            config.Configurer.ConfigureProperty<XmlMessageSerializer>(x => x.SkipWrappingElementForSingleMessages, dontWrapSingleMessages);

            if(!string.IsNullOrEmpty(nameSpace))
                config.Configurer.ConfigureProperty<XmlMessageSerializer>(x => x.Namespace, nameSpace);

            return config;
        }
    }
}
