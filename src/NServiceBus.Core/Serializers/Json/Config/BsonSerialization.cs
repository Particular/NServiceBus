namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;

    /// <summary>
    /// Used to control Bson as a message serializer
    /// </summary>
    public class BsonSerialization : Feature
    {
        
        internal BsonSerialization()
        {
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<BsonMessageSerializer>(DependencyLifecycle.SingleInstance);
        }
    }
}