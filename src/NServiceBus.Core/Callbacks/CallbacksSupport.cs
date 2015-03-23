namespace NServiceBus.Features
{
    using NServiceBus.Callbacks;

    class CallbacksSupport : Feature
    {
        public CallbacksSupport()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<RequestResponseMessageLookup>(DependencyLifecycle.SingleInstance);
            context.Pipeline.Register<RequestResponseInvocationBehavior.Registration>();
        }
    }
}