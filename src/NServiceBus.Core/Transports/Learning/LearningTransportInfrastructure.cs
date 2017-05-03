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

                if (!parent.Exists)
                {
                    // throw for now. if we discover there is an edge then we can fix it in a patch.
                    throw new Exception("Couldn't find the solution directory for the learning transport.");
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
            return new TransportSendInfrastructure(() => new LearningTransportDispatcher(storagePath), () => Task.FromResult(StartupCheckResult.Success));
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
            var endpoint = logicalAddress.EndpointInstance.Endpoint;
            PathChecker.ThrowForBadPath(endpoint, "endpoint name");

            var discriminator = logicalAddress.EndpointInstance.Discriminator ?? "";
            PathChecker.ThrowForBadPath(discriminator, "endpoint discriminator");

            var qualifier = logicalAddress.Qualifier ?? "";
            PathChecker.ThrowForBadPath(qualifier, "address qualifier");

            return Path.Combine(endpoint, discriminator, qualifier);
        }

        string storagePath;
        SettingsHolder settings;
        internal static string StorageLocationKey = "LearningTransport.StoragePath";
    }
}