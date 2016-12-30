namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extensibility;
    using Routing;

    class UnicastPublishRouter : IUnicastPublishRouter
    {
        public UnicastPublishRouter(UnicastSubscriberTable subscriberTable, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.subscriberTable = subscriberTable;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public IEnumerable<UnicastRoutingStrategy> Route(Type messageType, IDistributionPolicy distributionPolicy, ContextBag contextBag)
        {
            var routes = subscriberTable.GetRoutesFor(messageType);

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionPolicy, routes);

            return selectedDestinations.Select(destination => new UnicastRoutingStrategy(destination));
        }

        HashSet<string> SelectDestinationsForEachEndpoint(IDistributionPolicy distributionPolicy, IEnumerable<UnicastRouteGroup> routeGroups)
        {
            //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
            var addresses = new HashSet<string>();

            foreach (var group in routeGroups)
            {
                if (group.EndpointName == null) //Routing targets that do not specify endpoint name
                {
                    //Send a message to each target as we have no idea which endpoint they represent
                    foreach (var subscriber in group.Routes)
                    {
                        foreach (var address in ResolveRoute(subscriber))
                        {
                            addresses.Add(address);
                        }
                    }
                }
                else
                {
                    var candidates = group.Routes.SelectMany(ResolveRoute).ToArray();
                    var selected = distributionPolicy.GetDistributionStrategy(group.EndpointName, DistributionStrategyScope.Publish).SelectReceiver(candidates);
                    addresses.Add(selected);
                }
            }

            return addresses;
        }

        IEnumerable<string> ResolveRoute(UnicastRoute route)
        {
            if (route.Instance != null)
            {
                yield return transportAddressTranslation(route.Instance);
            }
            else if (route.PhysicalAddress != null)
            {
                yield return route.PhysicalAddress;
            }
            else
            {
                foreach (var instance in endpointInstances.FindInstances(route.Endpoint))
                {
                    yield return transportAddressTranslation(instance);
                }
            }
        }

        UnicastSubscriberTable subscriberTable;
        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
    }
}