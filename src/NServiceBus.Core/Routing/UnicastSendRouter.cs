namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

        //TODO change to single return value
        public IEnumerable<UnicastRoutingStrategy> Route(Type messageType, IDistributionPolicy distributionPolicy)
        {
            var route = unicastRoutingTable.GetRouteFor(messageType);
            if (route == null)
            {
                return emptyRoute;
            }

            if (route.PhysicalAddress != null)
            {
                return new[]
                {
                    new UnicastRoutingStrategy(route.PhysicalAddress)
                };
            }

            if (route.Instance != null)
            {
                return new[]
                {
                    new UnicastRoutingStrategy(transportAddressTranslation(route.Instance))
                };
            }

            var instances = endpointInstances.FindInstances(route.Endpoint).ToArray();
            //TODO adjust distribution policy
            var selectedInstance = distributionPolicy.GetDistributionStrategy(route.Endpoint).SelectDestination(instances);
            return new[]
            {
                new UnicastRoutingStrategy(transportAddressTranslation(selectedInstance))
            };
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        UnicastRoutingTable unicastRoutingTable;
        static UnicastRoutingStrategy[] emptyRoute = new UnicastRoutingStrategy[0];
    }
}