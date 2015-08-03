namespace NServiceBus.Serializers
{
    using NServiceBus.Features;

    class ContentBasedDeserialization : Feature
    {
        public ContentBasedDeserialization()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageSerializerResolver>(DependencyLifecycle.SingleInstance);
        }
    }
}