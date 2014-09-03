namespace NServiceBus.Features
{
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Serializers.Json;

    class JsonSerialization : Feature
    {
        public JsonSerialization()
        {
            EnableByDefault();
            Prerequisite(this.ShouldSerializationFeatureBeEnabled, "JsonSerialization not enable since serialization definition not detected.");
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            var c = context.Container.ConfigureComponent<JsonMessageSerializer>(DependencyLifecycle.SingleInstance);

            context.Settings.ApplyTo<JsonMessageSerializer>((IComponentConfig)c);
        }
    }
}