namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Serialization;
    using Serializers.Json;

    /// <summary>
    /// Uses Bson as the message serialization.
    /// </summary>
    public class BsonSerialization : ConfigureSerialization
    {
        internal BsonSerialization()
        {
        }

        /// <summary>
        /// Specify the concrete implementation of <see cref="IMessageSerializer"/> type.
        /// </summary>
        protected override Type GetSerializerType(FeatureConfigurationContext context)
        {
            return typeof(BsonMessageSerializer);
        }
    }
}