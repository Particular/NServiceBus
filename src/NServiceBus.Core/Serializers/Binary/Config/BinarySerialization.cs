namespace NServiceBus.Features
{
    using Serializers.Binary;

    public class BinarySerialization : Feature<Categories.Serializers>
    {
        public override void Initialize()
        {
            Configure.Component<SimpleMessageMapper>(DependencyLifecycle.SingleInstance);
            Configure.Component<BinaryMessageSerializer>(DependencyLifecycle.SingleInstance);
        }
    }
}