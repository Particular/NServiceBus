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

        public UnicastRoutingStrategy Route(Type messageType, IDistributionPolicy distributionPolicy)
        {
            var routes = unicastRoutingTable.GetRoutesFor(messageType);
            if (routes == null)
            {
                return null;
            }
            var candidates = routes.Routes.SelectMany(ResolveRoute).ToArray();
            if (candidates.Length == 0)
            {
                return null;
            }
            if (routes.EndpointName == null)
            {
                if (candidates.Length > 1)
                {
                    throw new Exception($"Cannot send a message to more than one destination: {messageType.FullName}");
                }
                return new UnicastRoutingStrategy(candidates[0]);
            }
            var selectedInstanceAddress = distributionPolicy.GetDistributionStrategy(routes.EndpointName, DistributionStrategyScope.Send).SelectReceiver(candidates);
            return new UnicastRoutingStrategy(selectedInstanceAddress);
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

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        UnicastRoutingTable unicastRoutingTable;
    }
}