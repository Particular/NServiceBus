using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Routing;

namespace NServiceBus.CentralizedRouting
{
    class RoutingTable
    {
        readonly RoutingFile routingFileAccess = new RoutingFile();

        public event EventHandler routingDataUpdated;

        public void Reload()
        {
            Endpoints = routingFileAccess.Read().ToArray();
            routingDataUpdated?.Invoke(this, EventArgs.Empty);
        }

        public EndpointRoutingConfiguration[] Endpoints { get; private set; }

        public List<EndpointInstance> EndpointInstances { get; private set; }
    }
}