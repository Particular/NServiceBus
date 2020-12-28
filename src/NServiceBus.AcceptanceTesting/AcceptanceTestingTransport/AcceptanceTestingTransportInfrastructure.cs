using System.Collections.Generic;
using NServiceBus.Transports;

namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Transport;

    class AcceptanceTestingTransportInfrastructure : TransportInfrastructure
    {
        public AcceptanceTestingTransportInfrastructure(HostSettings settings, AcceptanceTestingTransport transport, ReceiveSettings[] receivers)
        {
            this.settings = settings;
            this.transport = transport;
            this.receivers = receivers;

            if (transport.StorageLocation == null)
            {
                var solutionRoot = FindSolutionRoot();
                storagePath = Path.Combine(solutionRoot, ".attransport");
            }
        }

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

        public async Task ConfigureReceiveInfrastructure()
        {
            var pumps = new List<IMessageReceiver>();

            foreach (var receiver in receivers)
            {
                pumps.Add(await CreateReceiver(receiver).ConfigureAwait(false));
            }

            Receivers = Array.AsReadOnly(pumps.ToArray());
        }

        Task<IMessageReceiver> CreateReceiver(ReceiveSettings receiveSettings)
        {
            var errorQueueAddress = receiveSettings.ErrorQueue;
            PathChecker.ThrowForBadPath(errorQueueAddress, "ErrorQueueAddress");

            PathChecker.ThrowForBadPath(settings.Name, "endpoint name");

            ISubscriptionManager subscriptionManager = null;
            if (receiveSettings.UsePublishSubscribe)
            {
                subscriptionManager = new LearningTransportSubscriptionManager(storagePath, settings.Name, receiveSettings.ReceiveAddress);
            }
            var pump = new LearningTransportMessagePump(receiveSettings.Id, storagePath, settings.CriticalErrorAction,subscriptionManager, receiveSettings, transport.TransportTransactionMode);
            return Task.FromResult<IMessageReceiver>(pump);
        }

        public void ConfigureSendInfrastructure()
        {
            Dispatcher = new LearningTransportDispatcher(storagePath, int.MaxValue / 1024);
        }

        public override Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        readonly string storagePath;
        readonly HostSettings settings;
        readonly AcceptanceTestingTransport transport;
        readonly ReceiveSettings[] receivers;

        public const string StorageLocationKey = "AcceptanceTestingTransport.StoragePath";
        public const string UseNativePubSubKey = "AcceptanceTestingTransport.UseNativePubSub";
        public const string UseNativeDelayedDeliveryKey = "AcceptanceTestingTransport.UseNativeDelayedDelivery";
    }
}