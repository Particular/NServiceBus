namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Serialization;
    using Serializers.Binary;

    /// <summary>
    /// Uses Binary as the message serialization.
    /// </summary>
    public class BinarySerialization : ConfigureSerialization
    {
        internal BinarySerialization()
        {
        }

        /// <summary>
        /// Specify the concrete implementation of <see cref="IMessageSerializer"/> type.
        /// </summary>
        protected override Type GetSerializerType(FeatureConfigurationContext context)
        {
            return typeof(BinaryMessageSerializer);
        }

        /// <inheritdoc />
        protected override void RegisterSerializer(FeatureConfigurationContext context, Type serializerType)
        {
            base.RegisterSerializer(context, serializerType);

            context.Container.ConfigureComponent<SimpleMessageMapper>(DependencyLifecycle.SingleInstance);
        }
    }
}