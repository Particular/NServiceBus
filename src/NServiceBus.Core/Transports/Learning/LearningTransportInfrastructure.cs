using Janitor;

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Transport;

    [SkipWeaving]
    class LearningTransportInfrastructure : TransportInfrastructure
    {
        public LearningTransportInfrastructure(Transport.Settings settings, LearningTransport transportSettings,
            ReceiveSettings[] receivers)
        {
            this.settings = settings;
            this.transportSettings = transportSettings;

            if (string.IsNullOrWhiteSpace(storagePath = transportSettings.StorageDirectory))
            {
                storagePath = FindStoragePath();
            }

            ////TODO: pass push runtime settings as part of the settings but provide information whether it is a core default value or a user provided value.
            ////settings.ReceiveSettings.SetDefaultPushRuntimeSettings(new PushRuntimeSettings(1));

            this.receiveSettings = receivers;
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

        public Task<IPushMessages> CreateReceiver(ReceiveSettings receiveSettings)
        {
            var errorQueueAddress = receiveSettings.settings.ErrorQueue;
            PathChecker.ThrowForBadPath(errorQueueAddress, "ErrorQueueAddress");

            PathChecker.ThrowForBadPath(settings.Name, "endpoint name");

            IManageSubscriptions subscriptionManager = null;
            if (receiveSettings.UsePublishSubscribe)
            {
                subscriptionManager = new LearningTransportSubscriptionManager(storagePath, settings.Name, receiveSettings.ReceiveAddress);
            }
            var pump = new LearningTransportMessagePump(receiveSettings.Id, storagePath, settings.CriticalErrorAction,subscriptionManager, receiveSettings);
            return Task.FromResult<IPushMessages>(pump);
        }

        public void ConfigureSendInfrastructure()
        {
            var maxPayloadSize = transportSettings.RestrictPayloadSize ? 64 : int.MaxValue / 1024; //64 kB is the max size of the ASQ transport

            Dispatcher = new LearningTransportDispatcher(storagePath, maxPayloadSize);
        }

        public async Task ConfigureReceiveInfrastructure()
        {
            var pumps = new List<IPushMessages>();

            foreach (var receiveSetting in receiveSettings)
            {
                pumps.Add(await CreateReceiver(receiveSetting).ConfigureAwait(false));
            }

            Receivers = pumps.ToArray();
        }

        readonly string storagePath;
        readonly Transport.Settings settings;
        readonly LearningTransport transportSettings;
        ReceiveSettings[] receiveSettings;

        const string DefaultLearningTransportDirectory = ".learningtransport";
        public override void Dispose()
        {
        }
    }
}
