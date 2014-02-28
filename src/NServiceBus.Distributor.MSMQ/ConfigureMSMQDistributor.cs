namespace NServiceBus
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using Logging;
    using Settings;

    /// <summary>
    /// Extension methods to configure Distributor.
    /// </summary>
    public static class ConfigureMSMQDistributor
    {
        internal static bool DistributorConfiguredToRunOnThisEndpoint()
        {
            return SettingsHolder.GetOrDefault<bool>("Distributor.Enabled");
        }

        internal static bool WorkerRunsOnThisEndpoint()
        {
            return SettingsHolder.GetOrDefault<bool>("Worker.Enabled");
        }

        /// <summary>
        /// Configure this endpoint as both a Distributor and a Worker.
        /// </summary>
        public static Configure AsMSMQMasterNode(this Configure config)
        {
            return config.RunMSMQDistributor();
        }

        /// <summary>
        ///     Configure the distributor to run on this endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <param name="withWorker"><value>true</value> if this endpoint should enlist as a worker, otherwise <value>false</value>. Default is <value>true</value>.</param>
        public static Configure RunMSMQDistributor(this Configure config, bool withWorker = true)
        {
            Distributor.MSMQ.Config.DistributorInitializer.Init(withWorker);

            if (withWorker)
            {
                Distributor.MSMQ.Config.WorkerInitializer.Init();
            }


            return config;
        }

        /// <summary>
        ///     Enlist Worker with Master node defined in the config.
        /// </summary>
        public static Configure EnlistWithMSMQDistributor(this Configure config)
        {
            ValidateMasterNodeConfigurationForWorker();

            Distributor.MSMQ.Config.WorkerInitializer.Init();

            return config;
        }

        static void ValidateMasterNodeConfigurationForWorker()
        {
            var masterNodeName = Configure.Instance.GetMasterNode();

            if (masterNodeName == null)
            {
                throw new ConfigurationErrorsException(
                    "When defining Worker profile, 'MasterNodeConfig' section must be defined and the 'Node' entry should point to a valid host name.");
            }

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
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return null;
            }
            try
            {
                var hostIPs = Dns.GetHostAddresses(hostName);
                var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                if (hostIPs.Any(hostIp => (IPAddress.IsLoopback(hostIp) || (localIPs.Contains(hostIp)))))
                {
                    return true;
                }
            }
            catch
            {
                return null;
            }
            return false;
        }

        static ILog logger = LogManager.GetLogger(typeof(ConfigureMSMQDistributor));
    }
}