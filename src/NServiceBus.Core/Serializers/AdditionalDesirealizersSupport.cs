namespace NServiceBus.Serializers
{
    using NServiceBus.Features;

    class AdditionalDesirealizersSupport : Feature
    {
        public AdditionalDesirealizersSupport()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageDeserializerResolver>(DependencyLifecycle.SingleInstance);
        }
    }
}