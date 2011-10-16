﻿namespace NServiceBus
{
    public static class RoutingConfig
    {
        public static Configure AsMasterNode(this Configure config)
        {
            masterNode = true;
            return config;
        }

        public static bool IsConfiguredAsMasterNode { get { return masterNode; } }
        
        private static bool masterNode;
    }
}
