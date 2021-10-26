namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class AcceptanceTestingTransportInfrastructure : TransportInfrastructure
    {
        public AcceptanceTestingTransportInfrastructure(HostSettings settings, AcceptanceTestingTransport transportSettings, ReceiveSettings[] receiverSettings)
        {
            this.settings = settings;
            this.transportSettings = transportSettings;
            this.receiverSettings = receiverSettings;

            storagePath = transportSettings.StorageLocation ?? Path.Combine(FindSolutionRoot(), ".attransport");
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

        public async Task ConfigureReceivers()
        {
            var receivers = new Dictionary<string, IMessageReceiver>();

            foreach (var receiverSetting in receiverSettings)
            {
                receivers.Add(receiverSetting.Id, await CreateReceiver(receiverSetting).ConfigureAwait(false));
            }

            Receivers = receivers;
        }

        Task<IMessageReceiver> CreateReceiver(ReceiveSettings receiveSettings)
        {
            var errorQueueAddress = receiveSettings.ErrorQueue;
            PathChecker.ThrowForBadPath(errorQueueAddress, "ErrorQueueAddress");

            PathChecker.ThrowForBadPath(settings.Name, "endpoint name");

            var queueAddress = ToTransportAddress(receiveSettings.ReceiverName);

            ISubscriptionManager subscriptionManager = null;
            if (receiveSettings.UsePublishSubscribe && transportSettings.SupportsPublishSubscribe)
            {
                subscriptionManager = new LearningTransportSubscriptionManager(storagePath, settings.Name, queueAddress);
            }
            var pump = new LearningTransportMessagePump(receiveSettings.Id, queueAddress, storagePath, settings.CriticalErrorAction, subscriptionManager, receiveSettings, transportSettings.TransportTransactionMode);
            return Task.FromResult<IMessageReceiver>(pump);
        }

        public void ConfigureDispatcher()
        {
            Dispatcher = new LearningTransportDispatcher(storagePath, int.MaxValue / 1024);
        }

        public override Task Shutdown(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public override string ToTransportAddress(QueueAddress address)
        {
            var baseAddress = address.BaseAddress;
            PathChecker.ThrowForBadPath(baseAddress, "endpoint name");

            var discriminator = address.Discriminator;

            if (!string.IsNullOrEmpty(discriminator))
            {
                PathChecker.ThrowForBadPath(discriminator, "endpoint discriminator");

                baseAddress += "-" + discriminator;
            }

            var qualifier = address.Qualifier;

            if (!string.IsNullOrEmpty(qualifier))
            {
                PathChecker.ThrowForBadPath(qualifier, "address qualifier");

                baseAddress += "-" + qualifier;
            }

            return baseAddress;
        }

        readonly string storagePath;
        readonly HostSettings settings;
        readonly AcceptanceTestingTransport transportSettings;
        readonly ReceiveSettings[] receiverSettings;
    }
}