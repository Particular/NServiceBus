namespace NServiceBus.Serializers.XML.Config
{
    using System.Configuration;
    using Settings;

    /// <summary>
    /// Settings for the xml message serializer
    /// </summary>
    public class XmlSerializationSettings
    {
        /// <summary>
        /// Tells the serializer to not wrap properties which have either XDocument or XElement with a "PropertyName" element.
        /// By default the xml serializer serializes the following message
        /// </summary>
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
        public XmlSerializationSettings DontWrapRawXml()
        {
            SettingsHolder.SetProperty<XmlMessageSerializer>(s => s.SkipWrappingRawXml, true);

            return this;
        }

        /// <summary>
        /// Configures the serializer to use a custom namespace. (http://tempuri.net) is the default.
        /// <para>If the provided namespace ends with trailing forward slashes, those will be removed on the fly.</para>
        /// </summary>
        public XmlSerializationSettings Namespace(string namespaceToUse)
        {
            if(string.IsNullOrEmpty(namespaceToUse))
                throw new ConfigurationErrorsException("Can't use a null or empty string as the xml namespace");

            SettingsHolder.SetProperty<XmlMessageSerializer>(s => s.Namespace, namespaceToUse);

            return this;
        }

        /// <summary>
        /// Tells the serializer to sanitize the input data from illegal characters
        /// </summary>
        public XmlSerializationSettings SanitizeInput()
        {
            SettingsHolder.SetProperty<XmlMessageSerializer>(s => s.SanitizeInput, true);

            return this;
        }
    }
}