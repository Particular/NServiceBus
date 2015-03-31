#pragma warning disable 1591
namespace NServiceBus
{
    using System.Text;
    using NServiceBus.Serialization;
    using NServiceBus.Serializers.Json;

    public static class JsonSerializerConfigurationExtensions
    {
        /// <summary>
        /// Configures the encoding of JSON stream
        /// </summary>
        /// <param name="config">The configuration object</param>
        /// <param name="encoding">Encoding to use for serialization and deserialization</param>
        public static void Encoding(this SerializationExtentions<JsonSerializer> config, Encoding encoding)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNull(encoding, "encoding");
            config.Settings.SetProperty<JsonMessageSerializer>(s => s.Encoding, encoding);
        }
    }
}