namespace NServiceBus.Features
{
    using Serializers.Binary;

    /// <summary>
    /// Uses Binary as the message serialization.
    /// </summary>
    class BinarySerialization : Feature
    {
        
        internal BinarySerialization()
        {
            EnableByDefault();
            Prerequisite(this.ShouldSerializationFeatureBeEnabled, "BinarySerialization not enable since serialization definition not detected.");
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