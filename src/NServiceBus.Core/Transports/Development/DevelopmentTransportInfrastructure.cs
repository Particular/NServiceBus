namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Performance.TimeToBeReceived;
    using Routing;
    using Settings;
    using Transport;

    class DevelopmentTransportInfrastructure : TransportInfrastructure
    {
        public DevelopmentTransportInfrastructure(SettingsHolder settings)
        {
            this.settings = settings;

            var solutionRoot = FindSolutionRoot();
            storagePath = Path.Combine(solutionRoot, ".devtransport");
        }

        string FindSolutionRoot()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            do
            {
                if (Directory.EnumerateFiles(directory).Any(f => f.EndsWith(".sln")))
                {
                    return directory;
                }

                var di = Directory.GetParent(directory);

                if (!di.Exists)
                {
                    throw new Exception("Couldn't find your solution directory, please configure a storage path for the development transport using TBD(myPath)");
                }

                directory = di.FullName;

            } while (true);
        }

        public override IEnumerable<Type> DeliveryConstraints { get; } = new[]
        {
            typeof(DiscardIfNotReceivedBefore),
            typeof(NonDurableDelivery)
        };

        public override TransportTransactionMode TransactionMode => TransportTransactionMode.SendsAtomicWithReceive;

        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Multicast, OutboundRoutingType.Unicast);

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            return new TransportReceiveInfrastructure(() => new DevelopmentTransportMessagePump(storagePath), () => new DevelopmentTransportQueueCreator(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            return new TransportSendInfrastructure(() => new DevelopmentTransportDispatcher(storagePath), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            return new TransportSubscriptionInfrastructure(() => new DevelopmentTransportSubscriptionManager(storagePath, settings.EndpointName(), settings.LocalAddress()));
        }

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
        {
            return instance;
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return Path.Combine(logicalAddress.EndpointInstance.Endpoint,
                logicalAddress.EndpointInstance.Discriminator ?? "",
                logicalAddress.Qualifier ?? "");
        }

        string storagePath;

        SettingsHolder settings;
    }
}