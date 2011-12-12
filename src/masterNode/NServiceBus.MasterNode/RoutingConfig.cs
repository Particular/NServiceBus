namespace NServiceBus
{
    public static class RoutingConfig
    {
        public static Configure AsMasterNode(this Configure config)
        {
            masterNode = true;
            return config;
        }

        public static bool IsConfiguredAsMasterNode(this Configure config)
        {
            return masterNode;
        }
  
        static bool masterNode;
    }
}
