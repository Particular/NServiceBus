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
        /// <summary>
        /// Specify the concrete implementation of <see cref="IMessageSerializer"/> type.
        /// </summary>
        protected override Type GetSerializerType(FeatureConfigurationContext context)
        {
            return typeof(BinaryMessageSerializer);
        }

        /// <summary>
        /// Registeres the specified implementation of <see cref="IMessageSerializer"/>
        /// </summary>
        protected override void RegisterSerializer(FeatureConfigurationContext context)
        {
            base.RegisterSerializer(context);

            context.Container.ConfigureComponent<SimpleMessageMapper>(DependencyLifecycle.SingleInstance);
        }
    }
}