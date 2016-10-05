namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Routing;
    using Transport;

    class Receiving : Feature
    {
        internal Receiving()
        {
            EnableByDefault();
            Prerequisite(c => !c.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Endpoint is configured as send-only");
            Defaults(s =>
            {
                var transportInfrastructure = s.Get<TransportInfrastructure>();
                var discriminator = s.GetOrDefault<string>("EndpointInstanceDiscriminator");
                var baseQueueName = s.GetOrDefault<string>("BaseInputQueueName") ?? s.EndpointName();

                var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(s.EndpointName()));

                var mainLogicalAddress = LogicalAddress.CreateLocalAddress(baseQueueName, mainInstance.Properties);
                s.SetDefault<LogicalAddress>(mainLogicalAddress);

                var mainAddress = transportInfrastructure.ToTransportAddress(mainLogicalAddress);
                s.SetDefault("NServiceBus.SharedQueue", mainAddress);

                if (discriminator != null)
                {
                    var instanceSpecificAddress = transportInfrastructure.ToTransportAddress(mainLogicalAddress.CreateIndividualizedAddress(discriminator));
                    s.SetDefault("NServiceBus.EndpointSpecificQueue", instanceSpecificAddress);
                }
            });
        }

        /// <summary>
        /// <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var inboundTransport = context.Settings.Get<InboundTransport>();

            context.Settings.Get<QueueBindings>().BindReceiving(context.Settings.LocalAddress());

            var instanceSpecificQueue = context.Settings.InstanceSpecificQueue();
            if (instanceSpecificQueue != null)
            {
                context.Settings.Get<QueueBindings>().BindReceiving(instanceSpecificQueue);
            }

            var lazyReceiveConfigResult = new Lazy<TransportReceiveInfrastructure>(() => inboundTransport.Configure(context.Settings));
            context.Container.ConfigureComponent(b => lazyReceiveConfigResult.Value.MessagePumpFactory(), DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent(b => lazyReceiveConfigResult.Value.QueueCreatorFactory(), DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(new PrepareForReceiving(lazyReceiveConfigResult));
        }

        class PrepareForReceiving : FeatureStartupTask
        {
            public PrepareForReceiving(Lazy<TransportReceiveInfrastructure> lazy)
            {
                this.lazy = lazy;
            }

            protected override async Task OnStart(IMessageSession session)
            {
                var result = await lazy.Value.PreStartupCheck().ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
                }
            }

            protected override Task OnStop(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }

            readonly Lazy<TransportReceiveInfrastructure> lazy;
        }
    }
}