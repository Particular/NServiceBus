namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Transport;


    class LearningTransportInfrastructure : TransportInfrastructure
    {
        public LearningTransportInfrastructure(TransportSettings settings, LearningTransport transportSettings)
        {
            this.settings = settings;
            this.transportSettings = transportSettings;

            if (string.IsNullOrWhiteSpace(storagePath = transportSettings.StorageDirectory))
            {
                storagePath = FindStoragePath();
            }

            ////TODO: pass push runtime settings as part of the settings but provide information whether it is a core default value or a user provided value.
            ////settings.ReceiveSettings.SetDefaultPushRuntimeSettings(new PushRuntimeSettings(1));

        }

        public override bool SupportsTTBR { get; } = true;

        public override TransportTransactionMode TransactionMode => TransportTransactionMode.SendsAtomicWithReceive;

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

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure(ReceiveSettings receiveSettings)
        {
            var errorQueueAddress = receiveSettings.ErrorQueueAddress;
            PathChecker.ThrowForBadPath(errorQueueAddress, "ErrorQueueAddress");

            return new TransportReceiveInfrastructure(() => new LearningTransportMessagePump(storagePath, settings.CriticalErrorAction), () => new LearningTransportQueueCreator(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            var maxPayloadSize = transportSettings.RestrictPayloadSize ? 64 : int.MaxValue / 1024; //64 kB is the max size of the ASQ transport

            return new TransportSendInfrastructure(() => new LearningTransportDispatcher(storagePath, maxPayloadSize), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure(SubscriptionSettings subscriptionSettings)
        {
            return new TransportSubscriptionInfrastructure(() =>
            {
                var endpointName = settings.EndpointName;
                PathChecker.ThrowForBadPath(endpointName.Name, "endpoint name");

                var localAddress = subscriptionSettings.LocalAddress;
                PathChecker.ThrowForBadPath(localAddress, "localAddress");

                return new LearningTransportSubscriptionManager(storagePath, endpointName.Name, localAddress);
            });
        }

        public override EndpointAddress BuildLocalAddress(string queueName) => new EndpointAddress(queueName, null, new Dictionary<string, string>(), null);

        public override string ToTransportAddress(EndpointAddress endpointAddress)
        {
            var address = endpointAddress.Endpoint;
            PathChecker.ThrowForBadPath(address, "endpoint name");

            var discriminator = endpointAddress.Discriminator;

            if (!string.IsNullOrEmpty(discriminator))
            {
                PathChecker.ThrowForBadPath(discriminator, "endpoint discriminator");

                address += "-" + discriminator;
            }

            var qualifier = endpointAddress.Qualifier;

            if (!string.IsNullOrEmpty(qualifier))
            {
                PathChecker.ThrowForBadPath(qualifier, "address qualifier");

                address += "-" + qualifier;
            }

            return address;
        }

        readonly string storagePath;
        readonly TransportSettings settings;
        private readonly LearningTransport transportSettings;

        const string DefaultLearningTransportDirectory = ".learningtransport";
    }
}
