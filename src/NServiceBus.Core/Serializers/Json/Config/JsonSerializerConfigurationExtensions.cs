#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Text;
    using NServiceBus.Serialization;
    using Serializers.Json;
    using Settings;

    public static class JsonSerializerConfigurationExtensions
    {
        [ObsoleteEx(Replacement = "Use configuration.UseSerialization<JsonSerializer>(), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static SerializationSettings Json(this SerializationSettings settings)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Use configuration.UseSerialization<BsonSerializer>(), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static SerializationSettings Bson(this SerializationSettings settings)
        {
            throw new NotImplementedException();
        }

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