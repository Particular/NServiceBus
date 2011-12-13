namespace NServiceBus
{
    using MasterNode;

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
            if (string.IsNullOrEmpty(masterNodeAddress))
                masterNodeAddress = DefaultMasterNodeManager.DetermineMasterNode();

            return masterNodeAddress;
        }

        public static Address GetMasterNodeAddress(this Configure config)
        {
            return new Address(Configure.EndpointName,GetMasterNode(config));
        }


        static string masterNodeAddress;
        static bool isMasterNode;
    }
}
