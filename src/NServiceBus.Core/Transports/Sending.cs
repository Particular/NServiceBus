#pragma warning disable 1591
namespace NServiceBus
{
    using ObjectBuilder;
    using Routing;
    using Settings;
    using Transport;

    public class TransportComponent
    {
        public TransportComponent(TransportInfrastructure transportInfrastructure)
        {
            TransportInfrastructure = transportInfrastructure;
        }

        public IDispatchMessages Dispatcher { get; set; }

        public TransportInfrastructure TransportInfrastructure { get; set; }

        public LogicalAddress LogicalAddress { get; private set; }

        public string SharedQueue { get; private set; }

        public string EndpointSpecificQueue { private set; get; }

        public string EndpointName { get; private set; }

        public void Initialize(ReadOnlySettings settings, IConfigureComponents confgire)
        {
            var transport = settings.Get<OutboundTransport>();
            var sendInfrastructure = transport.Configure(settings);
            sendInfrastructure.PreStartupCheck();
            Dispatcher = sendInfrastructure.DispatcherFactory();

            EndpointName = settings.EndpointName();

            confgire.ConfigureComponent(() => Dispatcher, DependencyLifecycle.SingleInstance);

            var discriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
            var baseQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? EndpointName;

            var mainInstance = TransportInfrastructure.BindToLocalEndpoint(new EndpointInstance(EndpointName));

            var mainLogicalAddress = LogicalAddress.CreateLocalAddress(baseQueueName, mainInstance.Properties);
            LogicalAddress = mainLogicalAddress;
            //s.SetDefault<LogicalAddress>(mainLogicalAddress);

            var mainAddress = TransportInfrastructure.ToTransportAddress(mainLogicalAddress);
            SharedQueue = mainAddress;
            //s.SetDefault("NServiceBus.SharedQueue", mainAddress);

            if (discriminator != null)
            {
                var instanceSpecificAddress = TransportInfrastructure.ToTransportAddress(mainLogicalAddress.CreateIndividualizedAddress(discriminator));
                //s.SetDefault("NServiceBus.EndpointSpecificQueue", instanceSpecificAddress);
                EndpointSpecificQueue = instanceSpecificAddress;
            }
        }
    }
}