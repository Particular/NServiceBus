namespace NServiceBus
{
    using Features;
    using Serializers.XML;
    using Settings;

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
            SettingsHolder.SetProperty<XmlMessageSerializer>(s=>s.SanitizeInput,sanitizeInput);
            SettingsHolder.SetProperty<XmlMessageSerializer>(s => s.SkipWrappingElementForSingleMessages, dontWrapSingleMessages);
            SettingsHolder.SetProperty<XmlMessageSerializer>(s => s.SkipWrappingRawXml, dontWrapRawXml);

            if (!string.IsNullOrEmpty(nameSpace))
                SettingsHolder.SetProperty<XmlMessageSerializer>(s => s.Namespace, nameSpace);

            Feature.Enable<XmlSerialization>();

            return config;
        }
    }
}
