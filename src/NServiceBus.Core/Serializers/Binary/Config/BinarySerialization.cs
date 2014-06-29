namespace NServiceBus.Features
{
    using Serializers.Binary;

    /// <summary>
    /// Uses Binary as the message serialization.
    /// </summary>
    public class BinarySerialization : Feature
    {
        
        internal BinarySerialization()
        {
        }
        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SimpleMessageMapper>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<BinaryMessageSerializer>(DependencyLifecycle.SingleInstance);
        }
    }
}