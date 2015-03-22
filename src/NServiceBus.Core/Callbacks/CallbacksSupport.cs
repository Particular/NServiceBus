namespace NServiceBus
{
    using NServiceBus.Callbacks;
    using NServiceBus.Features;

    class CallbacksSupport : Feature
    {
        public CallbacksSupport()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<CallbackMessageLookup>(DependencyLifecycle.SingleInstance);
            context.Pipeline.Register<Callbacks.CallbackInvocationBehavior.Registration>();
        }
    }
}