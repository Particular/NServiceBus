namespace NServiceBus
{
    using System;
    using System.Configuration;
    using Config;

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    public static class ConfigureMasterNode
    {
        public static Configure AsMasterNode(this Configure config)
        {
            isMasterNode = true;
            return config;
        }

        public static bool IsConfiguredAsMasterNode(this Configure config)
        {
            return isMasterNode;
        }

        public static string GetMasterNode(this Configure config)
        {
            var section = Configure.GetConfigSection<MasterNodeConfig>();
            if (section != null)
                return section.Node;

            return null;
        }

        public static bool HasMasterNode(this Configure config)
        {
            return !string.IsNullOrEmpty(GetMasterNode(config));
        }

        public static Address GetMasterNodeAddress(this Configure config)
        {
            var unicastBusConfig = Configure.GetConfigSection<UnicastBusConfig>();

            //allow users to override data address in config
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.DistributorDataAddress))
            {
                return Address.Parse(unicastBusConfig.DistributorDataAddress);
            }

            var masterNode = GetMasterNode(config);

            if (string.IsNullOrWhiteSpace(masterNode))
            {
                return Address.Parse(Configure.EndpointName);
            }

            ValidateHostName(masterNode);

            return new Address(Configure.EndpointName, masterNode);
        }
        
        private static void ValidateHostName(string hostName)
        {
            if (Uri.CheckHostName(hostName) == UriHostNameType.Unknown)
                throw new ConfigurationErrorsException(string.Format("The 'Node' entry in MasterNodeConfig section of the configuration file: '{0}' is not a valid DNS name.", hostName));
        }

        static bool isMasterNode;
    }
}
