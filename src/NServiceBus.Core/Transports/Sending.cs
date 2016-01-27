namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Transports;

    class Sending : Feature
    {
        public Sending()
        {
            EnableByDefault();
            DependsOn<Transport>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transport = context.Settings.Get<OutboundTransport>();
            var sendConfigContext = transport.Configure(context.Settings);

            context.Container.ConfigureComponent(c =>
            {
                var dispatcher = sendConfigContext.DispatcherFactory();
                return dispatcher;
            }, DependencyLifecycle.SingleInstance);

            // TODO: I don't want to use the builder
            context.Container.ConfigureComponent(c =>
            {
                var sessionContextFactory = sendConfigContext.SessionContextFactory();
                return sessionContextFactory;
            }, DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(new PrepareForSending(sendConfigContext.PreStartupCheck));
        }
        
        class PrepareForSending : FeatureStartupTask
        {
            readonly Func<Task<StartupCheckResult>> preStartupCheck;

            public PrepareForSending(Func<Task<StartupCheckResult>> preStartupCheck)
            {
                this.preStartupCheck = preStartupCheck;
            }

            protected override async Task OnStart(IBusSession session)
            {
                var result = await preStartupCheck().ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    throw new Exception("Pre start-up check failed: "+ result.ErrorMessage);
                }
            }

            protected override Task OnStop(IBusSession session)
            {
                return TaskEx.CompletedTask;
            }
        }
    }
}