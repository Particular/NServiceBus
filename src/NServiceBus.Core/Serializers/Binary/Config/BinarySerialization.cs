namespace NServiceBus.Features
{
    using Serializers.Binary;

    public class BinarySerialization : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SimpleMessageMapper>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<BinaryMessageSerializer>(DependencyLifecycle.SingleInstance);
        }
    }
}