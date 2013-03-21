namespace NServiceBus
{
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
        /// <param name="dontWrapRawXml">Tells the serializer to not wrap properties which have either XDocument or XElement with a "PropertyName" element.
        /// By default the xml serializer serializes the following message
        /// <code>
        /// interface MyMessage { XDocument Property { get; set; } }
        /// </code>
        /// into the following structure
        /// <code>
        /// <MyMessage>
        ///     <Property>
        ///       ... Content of the XDocument
        ///     </Property>
        /// </MyMessage>
        /// </code>
        /// This flag allows to omit the property tag wrapping. Which results to
        /// <code>
        /// <MyMessage>
        ///       ... Content of the XDocument
        /// </MyMessage>
        /// </code>
        /// When this feature is enable the root element of the XDocument must match the name of the property. The following would not work and lead to deserialization error:
        /// <code>
        /// <MyMessage>
        ///       <Root>
        ///         ...
        ///       </Root>
        /// </MyMessage>
        /// </code>
        /// </param>
        /// <returns></returns>
        public static Configure XmlSerializer(this Configure config, string nameSpace = null, bool sanitizeInput = false, bool dontWrapSingleMessages = false, bool dontWrapRawXml = false)
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
            config.Configurer.ConfigureProperty<XmlMessageSerializer>(x => x.SkipWrappingRawXml, dontWrapRawXml);

            if(!string.IsNullOrEmpty(nameSpace))
                config.Configurer.ConfigureProperty<XmlMessageSerializer>(x => x.Namespace, nameSpace);

            return config;
        }
    }
}
