namespace NServiceBus
{
    using Serialization;

    /// <summary>
    /// Custom extensions for the <see cref="XmlSerializer" /> serializer.
    /// </summary>
    public static partial class XmlSerializationExtensions
    {
        /// <summary>
        /// Tells the serializer to not wrap properties which have either XDocument or XElement with a "PropertyName" element.
        /// By default the xml serializer serializes the following message.
        /// </summary>
        /// <code>
        /// interface MyMessage { XDocument Property { get; set; } }
        /// </code>
        /// into the following structure
        /// <code>
        /// <MyMessage>
        /// <Property>
        /// ... Content of the XDocument
        /// </Property>
        /// </MyMessage>
        /// </code>
        /// This flag allows to omit the property tag wrapping. Which results to
        /// <code>
        /// <MyMessage>
        /// ... Content of the XDocument
        /// </MyMessage>
        /// </code>
        /// When this feature is enable the root element of the XDocument must match the name of the property. The following would not work and lead to deserialization error:
        /// <code>
        /// <MyMessage>
        /// <Root>
        /// ...
        /// </Root>
        /// </MyMessage>
        /// </code>
        public static SerializationExtensions<XmlSerializer> DontWrapRawXml(this SerializationExtensions<XmlSerializer> config)
        {
            Guard.AgainstNull(nameof(config), config);

            config.Settings.Set(XmlSerializer.SkipWrappingRawXml, true);

            return config;
        }

        /// <summary>
        /// Configures the serializer to use a custom namespace. (http://tempuri.net) is the default.
        /// <para>If the provided namespace ends with trailing forward slashes, those will be removed on the fly.</para>
        /// </summary>
        /// <param name="config">The <see cref="SerializationExtensions{T}" /> to add a namespace to.</param>
        /// <param name="namespaceToUse">
        /// Namespace to use for interop scenarios.
        /// Note that this namespace is not validate or used for any logic inside NServiceBus.
        /// It is only for scenarios where a transport (or other infrastructure) requires message xml contents to have a specific
        /// namespace.
        /// </param>
        public static SerializationExtensions<XmlSerializer> Namespace(this SerializationExtensions<XmlSerializer> config, string namespaceToUse)
        {
            Guard.AgainstNull(nameof(config), config);

            config.Settings.Set(XmlSerializer.CustomNamespaceConfigurationKey, namespaceToUse);

            return config;
        }

        /// <summary>
        /// Tells the serializer to sanitize the input data from illegal characters.
        /// </summary>
        public static SerializationExtensions<XmlSerializer> SanitizeInput(this SerializationExtensions<XmlSerializer> config)
        {
            Guard.AgainstNull(nameof(config), config);

            config.Settings.Set(XmlSerializer.SanitizeInput, true);

            return config;
        }
    }
}