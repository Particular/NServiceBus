namespace NServiceBus
{
    using Features;
    using Routing;
    using Transport;

    class Receiving : Feature
    {
        internal Receiving()
        {
            EnableByDefault();
            Prerequisite(c => !c.Settings.Get<EndpointComponent>().IsSendOnly, "Endpoint is configured as send-only");
            Defaults(s =>
            {
                var transportInfrastructure = s.Get<TransportInfrastructure>();
                var discriminator = s.Get<EndpointComponent>().InstanceDiscriminator;
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
            context.Settings.Get<QueueBindings>().BindReceiving(context.Settings.LocalAddress());

            var instanceSpecificQueue = context.Settings.InstanceSpecificQueue();

            if (instanceSpecificQueue != null)
            {
                context.Settings.Get<QueueBindings>().BindReceiving(instanceSpecificQueue);
            }
        }
    }
}