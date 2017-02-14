namespace NServiceBus
{
    using System;
    using System.Linq;
    using Pipeline;
    using Routing;

    class UnicastSendRouter : IUnicastSendRouter
    {
        public UnicastSendRouter(UnicastRoutingTable unicastRoutingTable, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.unicastRoutingTable = unicastRoutingTable;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public UnicastRoutingStrategy Route(Type messageType, IDistributionPolicy distributionPolicy, IOutgoingSendContext sendContext)
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


            var instances = endpointInstances.FindInstances(route.Endpoint).Select(e => transportAddressTranslation(e)).ToArray();
            var distributionContext = new DistributionContext(instances, sendContext.Message, sendContext.MessageId, sendContext.Headers, transportAddressTranslation, sendContext.Extensions);
            var selectedInstanceAddress = distributionPolicy.GetDistributionStrategy(route.Endpoint, DistributionStrategyScope.Send).SelectDestination(distributionContext);
            return new UnicastRoutingStrategy(selectedInstanceAddress);
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        UnicastRoutingTable unicastRoutingTable;
    }
}