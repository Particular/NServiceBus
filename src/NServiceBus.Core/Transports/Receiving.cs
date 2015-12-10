namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using NServiceBus.Transports;

    class Receiving : Feature
    {
        internal Receiving()
        {
            EnableByDefault();
            DependsOn<UnicastBus>();
            Prerequisite(c => !c.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Endpoint is configured as send-only");
            Defaults(s =>
            {
                const string translationKey = "LogicalToTransportAddressTranslation";
                Func<LogicalAddress, string, string> emptyTranslation = (logicalAddress, defaultTranslation) => defaultTranslation;
                s.SetDefault(translationKey, emptyTranslation);
                var translation = s.Get<Func<LogicalAddress, string, string>>(translationKey);
                s.Set<LogicalToTransportAddressTranslation>(new LogicalToTransportAddressTranslation(s.Get<TransportDefinition>(), translation));
            });

            Defaults(s =>
            {
                var transport = s.Get<LogicalToTransportAddressTranslation>();
                var defaultTransportAddress = transport.Translate(s.RootLogicalAddress());
                s.SetDefault("NServiceBus.LocalAddress", defaultTransportAddress);
            });
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var inboundTransport = context.Settings.Get<InboundTransport>();

            context.Settings.Get<QueueBindings>().BindReceiving(context.Settings.LocalAddress());
            
            context.Container.RegisterSingleton(inboundTransport.Definition);

            var receiveConfigResult = inboundTransport.Configure(context.Settings);
            context.Container.ConfigureComponent(b => receiveConfigResult.MessagePumpFactory(), DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent(b => receiveConfigResult.QueueCreatorFactory(), DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(new PrepareForReceiving(receiveConfigResult.PreStartupCheck));
        }
        
        class PrepareForReceiving : FeatureStartupTask
        {
            readonly Func<Task<StartupCheckResult>> preStartupCheck;

            public PrepareForReceiving(Func<Task<StartupCheckResult>> preStartupCheck)
            {
                this.preStartupCheck = preStartupCheck;
            }

            protected override async Task OnStart(IBusSession session)
            {
                var result = await preStartupCheck().ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
                }
            }

            protected override Task OnStop(IBusSession session)
            {
                return TaskEx.Completed;
            }
        }
    }
}
