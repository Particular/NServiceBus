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

        public IEnumerable<UnicastRoutingStrategy> Route(Type messageType, IDistributionPolicy distributionPolicy)
        {
            var route = unicastRoutingTable.GetRouteFor(messageType);
            if (route == null)
            {
                return emptyRoute;
            }

            var routingTargets = route.Resolve(endpoint => endpointInstances.FindInstances(endpoint));

            var selectedDestinations = SelectDestination(distributionPolicy, routingTargets);

            return selectedDestinations
                .Select(destination => destination.Resolve(x => transportAddressTranslation(x)))
                .Distinct() //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
                .Select(destination => new UnicastRoutingStrategy(destination));
        }

        static IEnumerable<UnicastRoutingTarget> SelectDestination(IDistributionPolicy distributionPolicy, IEnumerable<UnicastRoutingTarget> destinations)
        {
            var destinationsByEndpoint = destinations
                .GroupBy(d => d.Endpoint, d => d);

            foreach (var group in destinationsByEndpoint)
            {
                if (group.Key == null) //Routing targets that do not specify endpoint name
                {
                    //Send a message to each target as we have no idea which endpoint they represent
                    foreach (var destination in group)
                    {
                        yield return destination;
                    }
                }
                else
                {
                    //Use the distribution strategy to select subset of instances of a given endpoint
                    foreach (var destination in distributionPolicy.GetDistributionStrategy(group.Key).SelectDestination(group.ToArray()))
                    {
                        yield return destination;
                    }
                }
            }
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        UnicastRoutingTable unicastRoutingTable;
        static UnicastRoutingStrategy[] emptyRoute = new UnicastRoutingStrategy[0];
    }
}