namespace NServiceBus.Serializers.SystemJson
{
    using System;
    using System.Text.Json;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    /// <summary>
    /// Enables message serialization using System.Text.Json.
    /// </summary>
    public class SystemJsonSerializer : SerializationDefinition
    {
        /// <summary>
        /// Provides a factory method for building a message serializer.
        /// </summary>
        public override Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings)
        {
            var options = settings.GetOrDefault<JsonSerializerOptions>();
            var readerOptions = settings.GetOrDefault<JsonReaderOptions>();
            var writerOptions = settings.GetOrDefault<JsonWriterOptions>();
            var contentTypeKey = settings.GetOrDefault<string>(SystemJsonConfigurationExtensions.ContentTypeSettingsKey);

            return mapper => new JsonMessageSerializer(options, writerOptions, readerOptions, contentTypeKey);
        }
    }
}
