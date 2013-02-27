namespace NServiceBus
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using Distributor.Config;
    using Logging;

    public static class ConfigureDistributor
    {
        public static bool DistributorEnabled(this Configure config)
        {
            return distributorEnabled;
        }

        public static bool DistributorConfiguredToRunOnThisEndpoint(this Configure config)
        {
            return distributorEnabled && distributorShouldRunOnThisEndpoint;
        }
        /// <summary>
        /// Return whether this endpoint contains a worker
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool WorkerRunsOnThisEndpoint(this Configure config)
        {
            return workerRunsOnThisEndpoint;
        }

        /// <summary>
        /// Configure the distributor to run on this endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <param name="withWorker">True if this endpoint should enlist as a worker</param>
        /// <returns></returns>
        public static Configure RunDistributor(this Configure config, bool withWorker = true)
        {
            distributorEnabled = true;
            distributorShouldRunOnThisEndpoint = true;

            DistributorInitializer.Init(withWorker);

            if (withWorker)
            {
                workerRunsOnThisEndpoint = true;
                WorkerInitializer.Init();
            }
                

            return config;
        }
        /// <summary>
        /// Starting the Distributor without a worker running on its endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure RunDistributorWithNoWorkerOnItsEndpoint(this Configure config)
        {
            config.RunDistributor(false);
            return config;
        }


        /// <summary>
        /// Enlist Worker with Master node defined in the config.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure EnlistWithDistributor(this Configure config)
        {
            workerRunsOnThisEndpoint = true;

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

        internal static bool? IsLocalIpAddress(string hostName)
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
        static bool distributorEnabled;
        static bool distributorShouldRunOnThisEndpoint;
        static bool workerRunsOnThisEndpoint;
        static ILog logger = LogManager.GetLogger(typeof(ConfigureDistributor));
    }
}