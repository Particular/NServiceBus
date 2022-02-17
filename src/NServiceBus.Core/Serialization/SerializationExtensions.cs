namespace NServiceBus.Serialization
{
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// This class provides implementers of serializers with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The serializer definition eg <see cref="XmlSerializer" />.</typeparam>
    public class SerializationExtensions<T> : ExposeSettings where T : SerializationDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SerializationExtensions{T}" />.
        /// </summary>
        public SerializationExtensions(SettingsHolder serializerSettings, SettingsHolder endpointConfigurationSettings) : base(serializerSettings)
            => EndpointConfigurationSettings = endpointConfigurationSettings;

        // provides access to the settings backing EndpointConfiguration. The settings provided by the 'Settings' property are isolated settings for the serializer.
        internal readonly SettingsHolder EndpointConfigurationSettings;
    }
}