using System.Configuration;
using System.Linq;
using System.Net;

namespace NServiceBus
{
    using System;

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
        /// Return whether a Worker should be running in the Distributor endpoint.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool WorkerShouldRunOnDistributorEndpoint(this Configure config)
        {
            return !workerShouldNotRunOnDistributorEndpoint;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure RunDistributor(this Configure config)
        {
            if (!config.IsConfiguredAsMasterNode())
                throw new InvalidOperationException("This endpoint needs to be configured as a master node in order to run the distributor");   

            distributorEnabled = true;
            distributorShouldRunOnThisEndpoint = true;

            return config;
        }
        /// <summary>
        /// Starting the Distributor without a worker running on its endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure RunDistributorWithNoWorkerOnItsEndpoint(this Configure config)
        {
            config.RunDistributor();
            workerShouldNotRunOnDistributorEndpoint = true;

            return config;
        }


        /// <summary>
        /// Enlist Worker with Master node defined in the config.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure EnlistWithDistributor(this Configure config)
        {
            distributorEnabled = true;
            if (config.IsConfiguredAsMasterNode())
                throw new InvalidOperationException("Worker endpoint should not be configured as a master node.");

            ValidateMasterNodeConfigurationForWorker(config);

            return config;
        }

        private static void ValidateMasterNodeConfigurationForWorker(Configure config)
        {
            var masterNodeName = config.GetMasterNode();

            if (masterNodeName == null)
                throw new ConfigurationErrorsException(
                    "When defining Worker profile, 'MasterNodeConfig' section must be defined and the 'Node' entry should point to a valid, non local, host name.");

            if (string.IsNullOrWhiteSpace(masterNodeName))
                throw new ConfigurationErrorsException(
                    string.Format("'MasterNodeConfig.Node' entry should point to a valid, non-local, host name: [{0}].", masterNodeName));

            if (IsLocalIpAddress(masterNodeName))
                throw new ConfigurationErrorsException(
                    string.Format("'MasterNodeConfig.Node' entry should point to a valid, non-local, host name. [{0}] points to a local host address.",
                        masterNodeName));
        }

        private static bool IsLocalIpAddress(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName)) return true;
            try
            {
                var hostIPs = Dns.GetHostAddresses(hostName);
                var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                if (hostIPs.Any(hostIp => (IPAddress.IsLoopback(hostIp) || (localIPs.Contains(hostIp)))))
                    return true;
            }
            catch
            {
                return false;
            }
            return false;
        }
        static bool distributorEnabled;
        static bool distributorShouldRunOnThisEndpoint;
        static bool workerShouldNotRunOnDistributorEndpoint;
    }
}