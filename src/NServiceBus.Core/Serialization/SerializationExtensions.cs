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
        /// Initializes a new instance of <see cref="SerializationExtensions{T}" />.
        /// </summary>
        public SerializationExtensions(SettingsHolder settings) : base(settings)
        {
        }
    }
}