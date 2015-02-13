#pragma warning disable 1591
namespace NServiceBus
{
    using System;
    using System.Text;
    using NServiceBus.Serialization;
    using Serializers.Json;

    public static class JsonSerializerConfigurationExtensions
    {
        /// <summary>
        /// Configures the encoding of JSON stream
        /// </summary>
        /// <param name="config">The configuration object</param>
        /// <param name="encoding">Encoding to use for serialization and deserialization</param>
        public static void Encoding(this SerializationExtentions<JsonSerializer> config, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            config.Settings.SetProperty<JsonMessageSerializer>(s => s.Encoding, encoding);
        }
    }
}