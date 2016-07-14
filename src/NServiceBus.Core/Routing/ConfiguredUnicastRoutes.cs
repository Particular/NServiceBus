namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Routing;

    class ConfiguredUnicastRoutes
    {
        public List<Action<UnicastRoutingTable, Type[]>> registrations = new List<Action<UnicastRoutingTable, Type[]>>();

        public void Add(Action<UnicastRoutingTable, Type[]> registration)
        {
            registrations.Add(registration);
        }
    }
}