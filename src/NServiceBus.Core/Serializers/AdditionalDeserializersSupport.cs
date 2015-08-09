namespace NServiceBus.Serializers
{
    using NServiceBus.Features;

    class AdditionalDeserializersSupport : Feature
    {
        public AdditionalDeserializersSupport()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageDeserializerResolver>(DependencyLifecycle.SingleInstance);
        }
    }
}