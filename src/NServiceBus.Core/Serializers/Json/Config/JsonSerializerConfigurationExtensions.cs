
#pragma warning disable 1591

namespace NServiceBus
{
    using System.Text;
    using Serialization;

    public static partial class JsonSerializerConfigurationExtensions
    {
        /// <summary>
        /// Configures the encoding of JSON stream.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="encoding">Encoding to use for serialization and deserialization.</param>
        public static void Encoding(this SerializationExtensions<JsonSerializer> config, Encoding encoding)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(encoding), encoding);
            config.Settings.Set("Serialization.Json.Encoding", encoding);
        }
    }
}