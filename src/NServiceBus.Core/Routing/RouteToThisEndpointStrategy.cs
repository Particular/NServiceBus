namespace NServiceBus.Routing
{
    using System.Collections.Generic;

    class RouteToThisEndpointStrategy : RoutingStrategy
    {
        RouteToThisEndpointStrategy() { }

        public static RoutingStrategy Instance = new RouteToThisEndpointStrategy();
        
        public override AddressTag Apply(Dictionary<string, string> headers)
        {
            // will never be called
            throw new System.NotImplementedException();
        }
    }
}