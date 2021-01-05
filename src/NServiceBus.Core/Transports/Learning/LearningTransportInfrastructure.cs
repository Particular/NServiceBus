using System.Collections.Generic;

namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Transport;

    class LearningTransportInfrastructure : TransportInfrastructure
    {
        public LearningTransportInfrastructure(HostSettings settings, LearningTransport transport, ReceiveSettings[] receivers)
        {
            this.settings = settings;
            this.transport = transport;

            if (string.IsNullOrWhiteSpace(storagePath = transport.StorageDirectory))
            {
                storagePath = FindStoragePath();
            }

            ////TODO: pass push runtime settings as part of the settings but provide information whether it is a core default value or a user provided value.
            ////settings.ReceiveSettings.SetDefaultPushRuntimeSettings(new PushRuntimeSettings(1));

            this.receivers = receivers;
        }

        static string FindStoragePath()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            while (true)
            {
                // Finding a solution file takes precedence
                if (Directory.EnumerateFiles(directory).Any(file => file.EndsWith(".sln")))
                {
                    return Path.Combine(directory, DefaultLearningTransportDirectory);
                }

                // When no solution file was found try to find a learning transport directory
                var learningTransportDirectory = Path.Combine(directory, DefaultLearningTransportDirectory);
                if (Directory.Exists(learningTransportDirectory))
                {
                    return learningTransportDirectory;
                }

                var parent = Directory.GetParent(directory);

                if (parent == null)
                {
                    // throw for now. if we discover there is an edge then we can fix it in a patch.
                    throw new Exception($"Unable to determine the storage directory path for the learning transport due to the absence of a solution file. Either create a '{DefaultLearningTransportDirectory}' directory in one of this project’s parent directories, or specify the path explicitly using the 'EndpointConfiguration.UseTransport<LearningTransport>().StorageDirectory()' API.");
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

        public Task<IMessageReceiver> CreateReceiver(ReceiveSettings receiveSettings)
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
            var maxPayloadSize = transport.RestrictPayloadSize ? 64 : int.MaxValue / 1024; //64 kB is the max size of the ASQ transport

            Dispatcher = new LearningTransportDispatcher(storagePath, maxPayloadSize);
        }

        string storagePath;
        HostSettings settings;
        ReceiveSettings[] receivers;
        LearningTransport transport;

        const string DefaultLearningTransportDirectory = ".learningtransport";
        public const string StorageLocationKey = "LearningTransport.StoragePath";
        public const string NoPayloadSizeRestrictionKey = "LearningTransport.NoPayloadSizeRestrictionKey";

        public override Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
