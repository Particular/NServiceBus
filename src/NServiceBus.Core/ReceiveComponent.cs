namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
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

        public LogicalAddress LogicalAddress { get; private set; }

        public string LocalAddress { get; private set; }

        public string InstanceSpecificQueue { get; private set; }

        public string ReceiveQueueName { get; private set; }

        public void Initialize(ReadOnlySettings settings, QueueBindings queueBindings)
        {
            this.settings = settings;

            if (isSendOnlyEndpoint)
            {
                return;
            }

            var discriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
            ReceiveQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? endpointName;

            var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(endpointName));

            LogicalAddress = LogicalAddress.CreateLocalAddress(ReceiveQueueName, mainInstance.Properties);

            LocalAddress = transportInfrastructure.ToTransportAddress(LogicalAddress);

            queueBindings.BindReceiving(LocalAddress);

            if (discriminator != null)
            {
                InstanceSpecificQueue = transportInfrastructure.ToTransportAddress(LogicalAddress.CreateIndividualizedAddress(discriminator));

                queueBindings.BindReceiving(InstanceSpecificQueue);
            }

            receiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
        }


        public IPushMessages BuildMessagePump()
        {
            return receiveInfrastructure.MessagePumpFactory();
        }

        public Task CreateQueuesIfNecessary(string username)
        {
            if (isSendOnlyEndpoint)
            {
                return TaskEx.CompletedTask;
            }

            if (!settings.CreateQueues())
            {
                return TaskEx.CompletedTask;
            }

            var queueCreator = receiveInfrastructure.QueueCreatorFactory();
            var queueBindings = settings.Get<QueueBindings>();

            return queueCreator.CreateQueueIfNecessary(queueBindings, username);
        }

        public async Task PerformPreStartupChecks()
        {
            if (isSendOnlyEndpoint)
            {
                return;
            }

            var result = await receiveInfrastructure.PreStartupCheck().ConfigureAwait(false);

            if (!result.Succeeded)
            {
                throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
            }
        }

        TransportReceiveInfrastructure receiveInfrastructure;
        TransportInfrastructure transportInfrastructure;
        readonly string endpointName;
        bool isSendOnlyEndpoint;
        ReadOnlySettings settings;
    }
}