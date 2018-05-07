namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Performance.TimeToBeReceived;
    using Routing;
    using Settings;
    using Transport;

    class LearningTransportInfrastructure : TransportInfrastructure
    {
        public LearningTransportInfrastructure(SettingsHolder settings)
        {
            this.settings = settings;

            if (!settings.TryGet(StorageLocationKey, out storagePath))
            {
                var solutionRoot = FindSolutionRoot();
                storagePath = Path.Combine(solutionRoot, ".learningtransport");
            }

            settings.SetDefault<MessageProcessingOptimizationExtensions.ConcurrencyLimit>(new MessageProcessingOptimizationExtensions.ConcurrencyLimit
            {
                MaxValue = 1
            });

            var errorQueueAddress = settings.ErrorQueueAddress();
            PathChecker.ThrowForBadPath(errorQueueAddress, "ErrorQueueAddress");
        }

        public override IEnumerable<Type> DeliveryConstraints { get; } = new[]
        {
            typeof(DiscardIfNotReceivedBefore),
            typeof(DelayDeliveryWith),
            typeof(DoNotDeliverBefore)
        };

        public override TransportTransactionMode TransactionMode => TransportTransactionMode.SendsAtomicWithReceive;

        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Multicast, OutboundRoutingType.Unicast);

        string FindSolutionRoot()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            while (true)
            {
                if (Directory.EnumerateFiles(directory).Any(file => file.EndsWith(".sln")))
                {
                    return directory;
                }

                var parent = Directory.GetParent(directory);

                if (parent == null)
                {
                    // throw for now. if we discover there is an edge then we can fix it in a patch.
                    throw new Exception("Couldn't find the solution directory for the learning transport. If the endpoint is outside the solution folder structure, make sure to specify a storage directory using the 'EndpointConfiguration.UseTransport<LearningTransport>().StorageDirectory()' API.");
                }

                directory = parent.FullName;
            }
        }

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            return new TransportReceiveInfrastructure(() => new LearningTransportMessagePump(storagePath), () => new LearningTransportQueueCreator(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            var maxPayloadSize = settings.GetOrDefault<bool>(NoPayloadSizeRestrictionKey) ? int.MaxValue / 1024 : 64; //64 kB is the max size of the ASQ transport

            return new TransportSendInfrastructure(() => new LearningTransportDispatcher(storagePath, maxPayloadSize), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            return new TransportSubscriptionInfrastructure(() =>
            {
                var endpointName = settings.EndpointName();
                PathChecker.ThrowForBadPath(endpointName, "endpoint name");

                var localAddress = settings.LocalAddress();
                PathChecker.ThrowForBadPath(localAddress, "localAddress");

                return new LearningTransportSubscriptionManager(storagePath, endpointName, localAddress);
            });
        }

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance) => instance;

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            var address = logicalAddress.EndpointInstance.Endpoint;
            PathChecker.ThrowForBadPath(address, "endpoint name");

            var discriminator = logicalAddress.EndpointInstance.Discriminator;

            if (!string.IsNullOrEmpty(discriminator))
            {
                PathChecker.ThrowForBadPath(discriminator, "endpoint discriminator");

                address += "-" + discriminator;
            }

            var qualifier = logicalAddress.Qualifier;

            if (!string.IsNullOrEmpty(qualifier))
            {
                PathChecker.ThrowForBadPath(qualifier, "address qualifier");

                address += "-" + qualifier;
            }

            return address;
        }

        string storagePath;
        SettingsHolder settings;

        public const string StorageLocationKey = "LearningTransport.StoragePath";
        public const string NoPayloadSizeRestrictionKey = "LearningTransport.NoPayloadSizeRestrictionKey";
    }
}