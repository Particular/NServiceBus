#nullable enable
namespace NServiceBus
{
    using System.Text.Json;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Serialization;
    using NServiceBus.Serializers.SystemJson;

    /// <summary>
    /// Extensions for <see cref="SerializationExtensions{T}"/> to manipulate how messages are serialized via System.Text.Json.
    /// </summary>
    public static class SystemJsonConfigurationExtensions
    {
        /// <summary>
        /// Configures the <see cref="JsonReaderOptions"/> to use.
        /// </summary>
        /// <param name="config">The <see cref="SerializationExtensions{T}"/> instance.</param>
        /// <param name="options">The <see cref="JsonReaderOptions"/> to use.</param>
        public static void ReaderOptions(this SerializationExtensions<SystemJsonSerializer> config, JsonReaderOptions options)
        {
            var settings = config.GetSettings().GetOrCreate<SystemJsonSerializerSettings>();
            settings.ReaderOptions = options;
        }

        /// <summary>
        /// Configures the <see cref="JsonWriterOptions"/> to use.
        /// </summary>
        /// <param name="config">The <see cref="SerializationExtensions{T}"/> instance.</param>
        /// <param name="options">The <see cref="JsonWriterOptions"/> to use.</param>
        public static void WriterOptions(this SerializationExtensions<SystemJsonSerializer> config, JsonWriterOptions options)
        {
            var settings = config.GetSettings().GetOrCreate<SystemJsonSerializerSettings>();
            settings.WriterOptions = options;
        }

        /// <summary>
        /// Configures the <see cref="JsonSerializerOptions"/> to use.
        /// </summary>
        /// <param name="config">The <see cref="SerializationExtensions{T}"/> instance.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
        public static void Options(this SerializationExtensions<SystemJsonSerializer> config, JsonSerializerOptions options)
        {
            var settings = config.GetSettings().GetOrCreate<SystemJsonSerializerSettings>();
            settings.SerializerOptions = options;
        }

        /// <summary>
        /// Configures string to use for <see cref="Headers.ContentType"/> headers.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="ContentTypes.Json"/>.
        /// This setting is required when this serializer needs to co-exist with other json serializers.
        /// </remarks>
        /// <param name="config">The <see cref="SerializationExtensions{T}"/> instance.</param>
        /// <param name="contentType">The content type added to the message that identifies how the message is serialized.</param>
        public static void ContentType(this SerializationExtensions<SystemJsonSerializer> config, string contentType)
        {
            Guard.AgainstNullAndEmpty(contentType, nameof(contentType));
            var settings = config.GetSettings().GetOrCreate<SystemJsonSerializerSettings>();
            settings.ContentType = contentType;
        }
    }
}
