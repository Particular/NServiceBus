namespace NServiceBus
{
    using System;
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

        public ReceiveConfiguration Configure(ReadOnlySettings settings)
        {
            if (isSendOnlyEndpoint)
            {
                return new ReceiveConfiguration(new LogicalAddress(), null, null, null, TransportTransactionMode.None, PushRuntimeSettings.Default, false, false);
            }

            var discriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
            var receiveQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? endpointName;

            var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(endpointName));

            var logicalAddress = LogicalAddress.CreateLocalAddress(receiveQueueName, mainInstance.Properties);

            var localAddress = transportInfrastructure.ToTransportAddress(logicalAddress);


            string instanceSpecificQueue = null;
            if (discriminator != null)
            {
                instanceSpecificQueue = transportInfrastructure.ToTransportAddress(logicalAddress.CreateIndividualizedAddress(discriminator));
            }

            var transactionMode = GetRequiredTransactionMode(settings);

            var pushRuntimeSettings = GetDequeueLimitations(settings);

            var purgeOnStartup = settings.GetOrDefault<bool>("Transport.PurgeOnStartup");

            return new ReceiveConfiguration(logicalAddress, receiveQueueName, localAddress, instanceSpecificQueue, transactionMode, pushRuntimeSettings, purgeOnStartup, true);
        }

        public async Task<ReceiveRuntime> InitializeRuntime(ReceiveConfiguration receiveConfiguration, QueueBindings queueBindings, TransportReceiveInfrastructure receiveInfrastructure, MainPipelineExecutor mainPipelineExecutor, IEventAggregator eventAggregator, IBuilder builder, CriticalError criticalError, string errorQueue)
        {
            var receiveRuntime = new ReceiveRuntime(receiveConfiguration, receiveInfrastructure, queueBindings);

            await receiveRuntime.Initialize(mainPipelineExecutor, eventAggregator, builder, criticalError, errorQueue).ConfigureAwait(false);

            return receiveRuntime;
        }

        PushRuntimeSettings GetDequeueLimitations(ReadOnlySettings settings)
        {
            if (settings.TryGet(out MessageProcessingOptimizationExtensions.ConcurrencyLimit concurrencyLimit))
            {
                return new PushRuntimeSettings(concurrencyLimit.MaxValue);
            }

            return PushRuntimeSettings.Default;
        }


        TransportTransactionMode GetRequiredTransactionMode(ReadOnlySettings settings)
        {
            var transportTransactionSupport = settings.Get<TransportInfrastructure>().TransactionMode;

            //if user haven't asked for a explicit level use what the transport supports
            if (!settings.TryGet(out TransportTransactionMode requestedTransportTransactionMode))
            {
                return transportTransactionSupport;
            }

            if (requestedTransportTransactionMode > transportTransactionSupport)
            {
                throw new Exception($"Requested transaction mode `{requestedTransportTransactionMode}` can't be satisfied since the transport only supports `{transportTransactionSupport}`");
            }

            return requestedTransportTransactionMode;
        }

        TransportInfrastructure transportInfrastructure;
        string endpointName;
        bool isSendOnlyEndpoint;
    }
}