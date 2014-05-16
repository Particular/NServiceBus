namespace NServiceBus.Features
{
    using Serializers.Binary;

    public class BinarySerialization : Feature<Categories.Serializers>
    {
        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<SimpleMessageMapper>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<BinaryMessageSerializer>(DependencyLifecycle.SingleInstance);
        }
    }
}