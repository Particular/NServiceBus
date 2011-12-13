namespace NServiceBus
{
    using Config;

    public static class MasterNodeConfiguration
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

        public static Address GetMasterNodeAddress(this Configure config)
        {
            var masterNode = GetMasterNode(config);
            
            if (string.IsNullOrWhiteSpace(masterNode))
                return Address.Parse(Configure.EndpointName);

            return new Address(Configure.EndpointName,GetMasterNode(config));
        }


        static bool isMasterNode;
    }
}
