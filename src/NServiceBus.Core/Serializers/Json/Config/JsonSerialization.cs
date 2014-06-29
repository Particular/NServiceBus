namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;

    /// <summary>
    /// Uses JSON as the message serialization.
    /// </summary>
    public class JsonSerialization : Feature
    {
        
        internal JsonSerialization()
        {
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<JsonMessageSerializer>(DependencyLifecycle.SingleInstance);
        }
    }
}