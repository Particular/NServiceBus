namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Serialization;
    using Serializers.Json;

    /// <summary>
    /// Uses JSON as the message serialization.
    /// </summary>
    public class JsonSerialization : ConfigureSerialization
    {
        /// <summary>
        /// Specify the concrete implementation of <see cref="IMessageSerializer"/> type.
        /// </summary>
        protected override Type GetSerializerType(FeatureConfigurationContext context)
        {
            return typeof(JsonMessageSerializer);
        }
    }
}