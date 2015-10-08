namespace NServiceBus.Features
{
    using System;
    using Serialization;
    using Serializers.Json;

    /// <summary>
    ///     Uses JSON as the message serialization.
    /// </summary>
    public class JsonSerialization : ConfigureSerialization
    {
        internal JsonSerialization()
        {
        }

        /// <summary>
        ///     Specify the concrete implementation of <see cref="IMessageSerializer" /> type.
        /// </summary>
        protected override Type GetSerializerType(FeatureConfigurationContext context)
        {
            return typeof(JsonMessageSerializer);
        }
    }
}