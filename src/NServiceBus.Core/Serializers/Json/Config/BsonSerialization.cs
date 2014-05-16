namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;

    public class BsonSerialization : Feature<Categories.Serializers>
    {
        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<BsonMessageSerializer>(DependencyLifecycle.SingleInstance);
        }
    }
}