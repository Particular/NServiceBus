#nullable enable
namespace NServiceBus
{
    using System;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;
    using NServiceBus.Serializers.SystemJson;
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
            var options = settings.GetOrDefault<SystemJsonSerializerSettings>() ?? new SystemJsonSerializerSettings();

            return mapper => new JsonMessageSerializer(options);
        }
    }
}
