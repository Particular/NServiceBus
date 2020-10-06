namespace NServiceBus.AcceptanceTesting
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

    class AcceptanceTestingTransportInfrastructure : TransportInfrastructure
    {
        public AcceptanceTestingTransportInfrastructure(SettingsHolder settings)
        {
            this.settings = settings;

            if (!settings.TryGet(UseNativeDelayedDeliveryKey, out nativeDelayedDelivery))
            {
                nativeDelayedDelivery = true;
            }

            if (!settings.TryGet(UseNativePubSubKey, out nativePubSub))
            {
                nativePubSub = true;
            }

            if (!settings.TryGet(StorageLocationKey, out storagePath))
            {
                var solutionRoot = FindSolutionRoot();
                storagePath = Path.Combine(solutionRoot, ".attransport");
            }

            var errorQueueAddress = settings.ErrorQueueAddress();
            PathChecker.ThrowForBadPath(errorQueueAddress, "ErrorQueueAddress");
        }

        public override IEnumerable<Type> DeliveryConstraints => nativeDelayedDelivery
            ? new[]
            {
                typeof(DiscardIfNotReceivedBefore),
                typeof(DelayDeliveryWith),
                typeof(DoNotDeliverBefore)
            }
            : new[]
            {
                typeof(DiscardIfNotReceivedBefore)
            };

        public override TransportTransactionMode TransactionMode => TransportTransactionMode.SendsAtomicWithReceive;

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
                    throw new Exception("Couldn't find the solution directory for the acceptance testing transport. If the endpoint is outside the solution folder structure, make sure to specify a storage directory using the 'EndpointConfiguration.UseTransport<AcceptanceTestingTransport>().StorageDirectory()' API.");
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
            return new TransportSendInfrastructure(() => new LearningTransportDispatcher(storagePath, int.MaxValue / 1024), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            if (!nativePubSub)
            {
                throw new NotSupportedException();
            }

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

        readonly string storagePath;
        readonly SettingsHolder settings;
        readonly bool nativePubSub;
        readonly bool nativeDelayedDelivery;

        public const string StorageLocationKey = "AcceptanceTestingTransport.StoragePath";
        public const string UseNativePubSubKey = "AcceptanceTestingTransport.UseNativePubSub";
        public const string UseNativeDelayedDeliveryKey = "AcceptanceTestingTransport.UseNativeDelayedDelivery";
    }
}