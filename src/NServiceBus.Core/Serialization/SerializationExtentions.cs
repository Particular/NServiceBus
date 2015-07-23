namespace NServiceBus.Serialization
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// This class provides implementers of serializers with an extension mechanism for custom settings via extention methods.
    /// </summary>
    /// <typeparam name="T">The serializer definition eg <see cref="JsonSerializer"/>, <see cref="XmlSerializer"/>, etc.</typeparam>
    public class SerializationExtentions<T> : ExposeSettings where T : SerializationDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SerializationExtentions{T}"/>.
        /// </summary>
        public SerializationExtentions(SettingsHolder settings) : base(settings)
        {
        }
    }
}