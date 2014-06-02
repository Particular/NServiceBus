namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;

    /// <summary>
    /// Uses Bson as the message serialization.
    /// </summary>
    public class BsonSerialization : Feature
    {
        
        internal BsonSerialization()
        {
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<BsonMessageSerializer>(DependencyLifecycle.SingleInstance);
        }
    }
}