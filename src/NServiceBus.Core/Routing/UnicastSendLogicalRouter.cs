namespace NServiceBus
{
    using System;
    using System.Linq;
    using Pipeline;
    using Routing;

    static partial class UnicastSend
    {
        public class LogicalRouter
        {
            public LogicalRouter(
                string sharedQueue,
                string baseInputQueueName,
                string endpointName,
                IDistributionPolicy defaultDistributionPolicy,
                UnicastRoutingTable unicastRoutingTable,
                EndpointInstances endpointInstances,
                Func<EndpointInstance, string> transportAddressTranslation)
            {
                this.endpointName = baseInputQueueName?? endpointName;
                this.defaultDistributionPolicy = defaultDistributionPolicy;
                this.sharedQueue = sharedQueue;
                this.unicastRoutingTable = unicastRoutingTable;
                this.endpointInstances = endpointInstances;
                this.transportAddressTranslation = transportAddressTranslation;
            }

            public virtual UnicastRoutingStrategy Route(IOutgoingSendContext context)
            {
                UnicastRoutingStrategy routingStrategy = null;

                var state = context.Extensions.GetOrCreate<State>();

                var thisEndpoint = state.Option == RouteOption.RouteToAnyInstanceOfThisEndpoint ? sharedQueue : null;

                if (thisEndpoint != null)
                {
                    var instances = endpointInstances.FindInstances(endpointName).Select(e => transportAddressTranslation(e)).ToArray();
                    var distributionContext = new DistributionContext(instances, context.Message, context.MessageId, context.Headers, transportAddressTranslation, context.Extensions);
                    var selectedInstanceAddress = defaultDistributionPolicy.GetDistributionStrategy(endpointName, DistributionStrategyScope.Send).SelectDestination(distributionContext);
                    routingStrategy = new UnicastRoutingStrategy(selectedInstanceAddress);
                }
                else
                {
                    var route = unicastRoutingTable.GetRouteFor(context.Message.MessageType);

                    if (route != null)
                    {
                        if (route.PhysicalAddress != null)
                        {
                            routingStrategy = new UnicastRoutingStrategy(route.PhysicalAddress);
                        }
                        else if (route.Instance != null)
                        {
                            routingStrategy = new UnicastRoutingStrategy(transportAddressTranslation(route.Instance));
                        }
                        else
                        {
                            var distributionPolicy = state.Option == RouteOption.RouteToSpecificInstance ? new SpecificInstanceDistributionPolicy(state.SpecificInstance, transportAddressTranslation) : defaultDistributionPolicy;
                            var instances = endpointInstances.FindInstances(route.Endpoint).Select(e => transportAddressTranslation(e)).ToArray();
                            var distributionContext = new DistributionContext(instances, context.Message, context.MessageId, context.Headers, transportAddressTranslation, context.Extensions);
                            var selectedInstanceAddress = distributionPolicy.GetDistributionStrategy(route.Endpoint, DistributionStrategyScope.Send).SelectDestination(distributionContext);
                            routingStrategy = new UnicastRoutingStrategy(selectedInstanceAddress);
                        }
                    }
                }

                return routingStrategy;
            }

            EndpointInstances endpointInstances;

            Func<EndpointInstance, string> transportAddressTranslation;

            UnicastRoutingTable unicastRoutingTable;
            string sharedQueue;
            IDistributionPolicy defaultDistributionPolicy;
            string endpointName;
        }
    }
}