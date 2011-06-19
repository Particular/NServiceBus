namespace NServiceBus
{
    public static class MasterNodeConfigurer
    {
        public static Configure AsMasterNode(this Configure config)
        {
            masterNode = true;
            return config;
        }

        public static bool ConfiguredAsMasterNode { get { return masterNode; } }
        private static bool masterNode;
    }
}
