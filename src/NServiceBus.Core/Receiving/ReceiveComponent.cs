namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Routing;
    using Settings;
    using Transport;

    class ReceiveComponent
    {
        public ReceiveComponent(string endpointName, bool isSendOnlyEndpoint, TransportInfrastructure transportInfrastructure)
        {
            this.endpointName = endpointName;
            this.isSendOnlyEndpoint = isSendOnlyEndpoint;
            this.transportInfrastructure = transportInfrastructure;
        }

        public ReceiveConfiguration Configure(ReadOnlySettings settings, QueueBindings queueBindings)
        {
            if (isSendOnlyEndpoint)
            {
                return new ReceiveConfiguration(new LogicalAddress(), null, null, null, false);
            }

            var discriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
            var receiveQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? endpointName;

            var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(endpointName));

            var logicalAddress = LogicalAddress.CreateLocalAddress(receiveQueueName, mainInstance.Properties);

            var localAddress = transportInfrastructure.ToTransportAddress(logicalAddress);

            queueBindings.BindReceiving(localAddress);

            string instanceSpecificQueue = null;
            if (discriminator != null)
            {
                instanceSpecificQueue = transportInfrastructure.ToTransportAddress(logicalAddress.CreateIndividualizedAddress(discriminator));

                queueBindings.BindReceiving(instanceSpecificQueue);
            }

            return new ReceiveConfiguration(logicalAddress, receiveQueueName, localAddress, instanceSpecificQueue, true);
        }

        public async Task<ReceiveRuntime> Initialize(ReadOnlySettings settings, ReceiveConfiguration receiveConfiguration, TransportReceiveInfrastructure receiveInfrastructure, MainPipelineExecutor mainPipelineExecutor, IEventAggregator eventAggregator, IBuilder builder, CriticalError criticalError)
        {
            var receiveRuntime = new ReceiveRuntime(settings, receiveConfiguration, receiveInfrastructure);

            await receiveRuntime.Initialize(mainPipelineExecutor, eventAggregator, builder, criticalError).ConfigureAwait(false);

            return receiveRuntime;
        }

        TransportInfrastructure transportInfrastructure;

        string endpointName;
        bool isSendOnlyEndpoint;
    }
}