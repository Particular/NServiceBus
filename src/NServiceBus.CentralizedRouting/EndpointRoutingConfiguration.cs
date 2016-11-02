using System;

namespace NServiceBus.CentralizedRouting
{
    class EndpointRoutingConfiguration
    {
        public string LogicalEndpointName { get; set; }

        public Type[] Commands { get; set; }

        public Type[] Events { get; set; }
    }
}