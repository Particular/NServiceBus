namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;

    class RoutingStrategyFactory
    {
        public RoutingStrategy Create(Dictionary<string, string> options)
        {
            string destination;

            if (options.TryGetValue("Destination", out destination))
            {
                return new DirectToTargetDestination(destination);
            }

            string eventType;

            if (options.TryGetValue("EventType", out eventType))
            {
                return new ToAllSubscribers(Type.GetType(eventType,true));
            }
         
            throw new Exception("Could not find routing strategy to deserialize");
        }
    }
}