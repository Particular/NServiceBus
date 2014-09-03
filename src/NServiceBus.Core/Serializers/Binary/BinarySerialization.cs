namespace NServiceBus.Features
{
    using Serializers.Binary;

    class BinarySerialization : Feature
    {
        
        public BinarySerialization()
        {
            EnableByDefault();
            Prerequisite(this.ShouldSerializationFeatureBeEnabled, "BinarySerialization not enable since serialization definition not detected.");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SimpleMessageMapper>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<BinaryMessageSerializer>(DependencyLifecycle.SingleInstance);
        }
    }
}