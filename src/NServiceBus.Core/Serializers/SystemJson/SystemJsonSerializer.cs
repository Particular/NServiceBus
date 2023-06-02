namespace NServiceBus.Serializers.SystemJson
{
    using System;
    using System.Text.Json;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    /// <summary>
    /// 
    /// </summary>
    public class SystemJsonSerializer : SerializationDefinition
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
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
