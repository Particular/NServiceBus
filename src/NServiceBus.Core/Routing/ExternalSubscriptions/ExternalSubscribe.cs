namespace NServiceBus.Routing.ExternalSubscriptions
{
    using System.Threading.Tasks;
    using Features;
    using Pipeline;

    /// <summary>
    /// 
    /// </summary>
    public class ExternalSubscribe : Feature
    {
        /// <summary>
        /// </summary>
        public ExternalSubscribe()
        {
            Prerequisite(c => !c.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't subscribe to events.");
            Prerequisite(c => c.Settings.GetOrDefault<IUnicastPublishProvider>() != null, $"{nameof(ExternalSubscribe)} is disabled because of no registered {nameof(IUnicastPublishProvider)}.");
        }

        /// <inheritdoc />
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");

            if (canReceive)
            {
                var provider = context.Settings.Get<IUnicastPublishProvider>();
                var publish = provider.Get(context);

                context.Container.ConfigureComponent(b => publish, DependencyLifecycle.SingleInstance);

                context.Pipeline.Register(b => new NopSubscribeTerminator(), "Provides nop implementation for externally controlled subscriptions.");
                context.Pipeline.Register(b => new NopUnsubscribeTerminator(), "Provides nop implementation for externally controlled subscriptions.");
            }
        }

        class NopUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
        {
            protected override Task Terminate(IUnsubscribeContext context)
            {
                return TaskEx.CompletedTask;
            }
        }

        class NopSubscribeTerminator : PipelineTerminator<ISubscribeContext>
        {
            protected override Task Terminate(ISubscribeContext context)
            {
                return TaskEx.CompletedTask;
            }
        }
    }
}