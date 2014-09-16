namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Serializers.Json;

    /// <summary>
    /// Uses JSON as the message serialization.
    /// </summary>
    public class JsonSerialization : Feature
    {
        
        internal JsonSerialization()
        {
            EnableByDefault();
            Prerequisite(this.ShouldSerializationFeatureBeEnabled, "JsonSerialization not enable since serialization definition not detected.");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            var c = context.Container.ConfigureComponent<JsonMessageSerializer>(DependencyLifecycle.SingleInstance);

            context.Settings.ApplyTo<JsonMessageSerializer>((IComponentConfig)c);
        }
    }
}