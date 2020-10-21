namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Performance.TimeToBeReceived;
    using Transport;

    class AcceptanceTestingTransportInfrastructure : TransportInfrastructure
    {
        public AcceptanceTestingTransportInfrastructure(ReceiveSettings[] receiveSettings, Settings settings,
            AcceptanceTestingTransport acceptanceTestingTransport)
        {
            this.receiveSettings = receiveSettings;
            this.settings = settings;

            if (string.IsNullOrWhiteSpace(storagePath = acceptanceTestingTransport.StorageDirectory))
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

        public Task<IPushMessages> CreateReceiver(ReceiveSettings receiveSettings)
        {
            var errorQueueAddress = receiveSettings.settings.ErrorQueue;
            PathChecker.ThrowForBadPath(errorQueueAddress, "ErrorQueueAddress");

            IManageSubscriptions subscriptionManager = null;
            if (receiveSettings.UsePublishSubscribe)
            {
                var endpointName = settings.Name;
                PathChecker.ThrowForBadPath(endpointName, "endpoint name");

                var localAddress = receiveSettings.LocalAddress;
                PathChecker.ThrowForBadPath(localAddress, "localAddress");

                subscriptionManager = new LearningTransportSubscriptionManager(storagePath, endpointName, localAddress);
            }

            var pump = new LearningTransportMessagePump(receiveSettings.Id, storagePath, settings.CriticalErrorAction, subscriptionManager, receiveSettings);

            return Task.FromResult<IPushMessages>(pump);
        }

        public void ConfigureSendInfrastructure()
        {
            Dispatcher = new LearningTransportDispatcher(storagePath, int.MaxValue / 1024);
        }

        public async Task ConfigureReceiveInfrastructure()
        {
            var pumps = new List<IPushMessages>();

            foreach (var receiveSetting in receiveSettings)
            {
                var pump = await CreateReceiver(receiveSetting).ConfigureAwait(false);

                pumps.Add(pump);
            }

            Receivers = pumps.ToArray();
        }

        readonly string storagePath;
        readonly ReceiveSettings[] receiveSettings;
        readonly Settings settings;
        public override void Dispose()
        {
            
        }
    }
}