namespace NServiceBus.Serialization
{
    using Configuration.AdvanceExtensibility;
    using Settings;

    /// <summary>
    /// This class provides implementers of serializers with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The serializer definition eg <see cref="JsonSerializer" />, <see cref="XmlSerializer" />, etc.</typeparam>
    public class SerializationExtensions<T> : ExposeSettings where T : SerializationDefinition
    {
        /// <summary>
        /// The instance of <see cref="SerializationDefinition"/> that was created as part of <see cref="SerializationConfigExtensions.AddDeserializer{T}(EndpointConfiguration,T)"/>.
        /// </summary>
        public T Definition { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="SerializationExtensions{T}" />.
        /// </summary>
        [ObsoleteEx(
             RemoveInVersion = "7.0",
             ReplacementTypeOrMember = "SerializationExtensions(SettingsHolder settings, T definition)")]
        public SerializationExtensions(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SerializationExtensions{T}" />.
        /// </summary>
        public SerializationExtensions(SettingsHolder settings, T definition) : base(settings)
        {
            Guard.AgainstNull(nameof(definition), definition);
            Definition = definition;
        }
    }
}