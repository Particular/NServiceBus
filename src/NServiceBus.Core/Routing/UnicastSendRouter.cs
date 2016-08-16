namespace NServiceBus
{
    using System;
    using System.Linq;
    using Routing;

    class UnicastSendRouter : IUnicastSendRouter
    {
        public UnicastSendRouter(UnicastRoutingTable unicastRoutingTable, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.unicastRoutingTable = unicastRoutingTable;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public UnicastRoutingStrategy Route(Type messageType, IDistributionPolicy distributionPolicy)
        {
            var route = unicastRoutingTable.GetRouteFor(messageType);
            if (route == null)
            {
                return null;
            }

            if (route.PhysicalAddress != null)
            {
                return new UnicastRoutingStrategy(route.PhysicalAddress);
            }

            if (route.Instance != null)
            {
                return new UnicastRoutingStrategy(transportAddressTranslation(route.Instance));
            }

            var instances = endpointInstances.FindInstances(route.Endpoint).ToArray();
            var selectedInstance = distributionPolicy.GetDistributionStrategy(route.Endpoint).SelectReceiver(instances);
            return new UnicastRoutingStrategy(transportAddressTranslation(selectedInstance));
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        UnicastRoutingTable unicastRoutingTable;
    }
}