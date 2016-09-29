namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Transport;

    class Sending : Feature
    {
        public Sending()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transport = context.Settings.Get<OutboundTransport>();
            var lazySendingConfigResult = new Lazy<TransportSendInfrastructure>(() => transport.Configure(context.Settings), LazyThreadSafetyMode.ExecutionAndPublication);
            context.Container.ConfigureComponent(c =>
            {
                var dispatcher = lazySendingConfigResult.Value.DispatcherFactory();
                return dispatcher;
            }, DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(new PrepareForSending(lazySendingConfigResult));
        }

        class PrepareForSending : FeatureStartupTask
        {
            public PrepareForSending(Lazy<TransportSendInfrastructure> lazy)
            {
                this.lazy = lazy;
            }

            protected override async Task OnStart(IMessageSession session)
            {
                var result = await lazy.Value.PreStartupCheck().ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    throw new Exception("Pre start-up check failed: " + result.ErrorMessage);
                }
            }

            protected override Task OnStop(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }

            readonly Lazy<TransportSendInfrastructure> lazy;
        }
    }
}