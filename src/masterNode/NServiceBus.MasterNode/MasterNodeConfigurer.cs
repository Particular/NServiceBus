namespace NServiceBus
{
    public static class RoutingConfig
    {
        public static Configure AsMasterNode(this Configure config)
        {
            masterNode = true;
            return config;
        }

        public static Configure DynamicNodeDiscovery(this Configure config)
        {
            dynamicNode = true;
            return config;
        }

        public static bool IsConfiguredAsMasterNode { get { return masterNode; } }
        public static bool IsDynamicNodeDiscoveryOn { get { return dynamicNode; } }
        
        private static bool masterNode;
        private static bool dynamicNode;
    }
}
