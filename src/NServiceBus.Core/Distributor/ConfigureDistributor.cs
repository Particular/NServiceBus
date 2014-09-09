namespace NServiceBus
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using Distributor.Config;
    using Logging;
    using Settings;

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    public static class ConfigureDistributor
    {
        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
        public static bool DistributorEnabled(this Configure config)
        {
            return config.DistributorConfiguredToRunOnThisEndpoint();
        }

        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
        public static bool DistributorConfiguredToRunOnThisEndpoint(this Configure config)
        {
            return SettingsHolder.GetOrDefault<bool>("Distributor.Enabled");
        }

        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
        public static bool WorkerRunsOnThisEndpoint(this Configure config)
        {
            return SettingsHolder.GetOrDefault<bool>("Worker.Enabled");
        }

        /// <summary>
        /// Configure the distributor to run on this endpoint
        /// </summary>
        /// <param name="withWorker">True if this endpoint should enlist as a worker</param>
        /// <param name="config">True if this endpoint should enlist as a worker</param>
        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
        public static Configure RunDistributor(this Configure config, bool withWorker = true)
        {
            DistributorInitializer.Init(withWorker);

            if (withWorker)
            {
                WorkerInitializer.Init();
            }

            return config;
        }

        /// <summary>
        /// Starting the Distributor without a worker running on its endpoint
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
        public static Configure RunDistributorWithNoWorkerOnItsEndpoint(this Configure config)
        {
            config.RunDistributor(false);
            return config;
        }

        /// <summary>
        /// Enlist Worker with Master node defined in the config.
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
        public static Configure EnlistWithDistributor(this Configure config)
        {
            ValidateMasterNodeConfigurationForWorker(config);

            WorkerInitializer.Init();

            return config;
        }

        private static void ValidateMasterNodeConfigurationForWorker(Configure config)
        {
            var masterNodeName = config.GetMasterNode();

            if (masterNodeName == null)
                throw new ConfigurationErrorsException(
                    "When defining Worker profile, 'MasterNodeConfig' section must be defined and the 'Node' entry should point to a valid host name.");

            switch (IsLocalIpAddress(masterNodeName))
            {
                case true:
                    Address.InitializeLocalAddress(Address.Local.SubScope(Guid.NewGuid().ToString()).ToString());
                    logger.WarnFormat("'MasterNodeConfig.Node' points to a local host name: [{0}]. Worker input address name is [{1}]. It is randomly and uniquely generated to allow multiple workers working from the same machine as the Distributor.", masterNodeName, Address.Local);
                    break;
                case false:
                    logger.InfoFormat("'MasterNodeConfig.Node' points to a non-local valid host name: [{0}].", masterNodeName);
                    break;
                case null:
                    throw new ConfigurationErrorsException(
                        string.Format("'MasterNodeConfig.Node' entry should point to a valid host name. Currently it is: [{0}].", masterNodeName));
            }
        }

        static bool? IsLocalIpAddress(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName)) return null;
            try
            {
                var hostIPs = Dns.GetHostAddresses(hostName);
                var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                if (hostIPs.Any(hostIp => (IPAddress.IsLoopback(hostIp) || (localIPs.Contains(hostIp)))))
                    return true;
            }
            catch
            {
                return null;
            }
            return false;
        }

        static ILog logger = LogManager.GetLogger(typeof(ConfigureDistributor));
    }
}