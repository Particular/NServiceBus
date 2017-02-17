namespace NServiceBus
{
    using System;
    using System.Linq;
    using Pipeline;
    using Routing;
    using Transport;

    class UnicastSendRouter
    {
        public enum RouteOption
        {
            None,
            ExplicitDestination,
            RouteToThisInstance,
            RouteToAnyInstanceOfThisEndpoint,
            RouteToSpecificInstance
        }

        public UnicastSendRouter(
            string baseInputQueueName,
            string endpointName,
            string instanceSpecificQueue,
            string distributorAddress,
            IDistributionPolicy defaultDistributionPolicy,
            UnicastRoutingTable unicastRoutingTable,
            EndpointInstances endpointInstances,
            Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.endpointName = baseInputQueueName ?? endpointName;
            this.instanceSpecificQueue = instanceSpecificQueue;
            this.distributorAddress = distributorAddress;
            this.defaultDistributionPolicy = defaultDistributionPolicy;
            this.unicastRoutingTable = unicastRoutingTable;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public virtual UnicastRoutingStrategy Route(IOutgoingSendContext context)
        {
            var state = context.Extensions.GetOrCreate<State>();
            var route = SelectRoute(state, context);
            return ResolveRoute(route, context);
        }

        UnicastRoute SelectRoute(State state, IOutgoingSendContext context)
        {
            switch (state.Option)
            {
                case RouteOption.ExplicitDestination:
                    return UnicastRoute.CreateFromPhysicalAddress(state.ExplicitDestination);
                case RouteOption.RouteToThisInstance:
                    return RouteToThisInstance();
                case RouteOption.RouteToAnyInstanceOfThisEndpoint:
                    return RouteToAnyInstanceOfThisEndpoint(context);
                case RouteOption.RouteToSpecificInstance:
                    return RouteToSpecificInstance(context, state.SpecificInstance);
                case RouteOption.None:
                    return RouteUsingTable(context);
                default:
                    throw new Exception($"Unsupported route option: {state.Option}");
            }
        }

        UnicastRoute RouteToThisInstance()
        {
            if (instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
            }
            return UnicastRoute.CreateFromPhysicalAddress(instanceSpecificQueue);
        }

        UnicastRoute RouteToSpecificInstance(IOutgoingSendContext context, string specificInstance)
        {
            var route = RouteUsingTable(context);
            if (route.Endpoint == null)
            {
                throw new Exception("Routing to a specific instance is only allowed if route is defined for a logical endpoint, not for an address or instance.");
            }
            return UnicastRoute.CreateFromEndpointInstance(new EndpointInstance(route.Endpoint, specificInstance));
        }

        UnicastRoute RouteToAnyInstanceOfThisEndpoint(IOutgoingSendContext context)
        {
            return IncomingMessageOriginatesFromDistributor(context)
                ? UnicastRoute.CreateFromPhysicalAddress(distributorAddress)
                : UnicastRoute.CreateFromEndpointName(endpointName);
        }

        UnicastRoute RouteUsingTable(IOutgoingSendContext context)
        {
            var route = unicastRoutingTable.GetRouteFor(context.Message.MessageType);
            if (route == null)
            {
                throw new Exception($"No destination specified for message: {context.Message.MessageType}");
            }
            return route;
        }

        bool IncomingMessageOriginatesFromDistributor(IOutgoingSendContext context)
        {
            IncomingMessage incomingMessage;
            return distributorAddress != null && context.Extensions.TryGet(out incomingMessage) && incomingMessage.Headers.ContainsKey(LegacyDistributorHeaders.WorkerSessionId);
        }

        UnicastRoutingStrategy ResolveRoute(UnicastRoute route, IOutgoingSendContext context)
        {
            if (route.PhysicalAddress != null)
            {
                return new UnicastRoutingStrategy(route.PhysicalAddress);
            }
            if (route.Instance != null)
            {
                return new UnicastRoutingStrategy(transportAddressTranslation(route.Instance));
            }
            var instances = endpointInstances.FindInstances(route.Endpoint).Select(e => transportAddressTranslation(e)).ToArray();
            var distributionContext = new DistributionContext(instances, context.Message, context.MessageId, context.Headers, transportAddressTranslation, context.Extensions);
            var selectedInstanceAddress = defaultDistributionPolicy.GetDistributionStrategy(route.Endpoint, DistributionStrategyScope.Send).SelectDestination(distributionContext);
            return new UnicastRoutingStrategy(selectedInstanceAddress);
        }

        string instanceSpecificQueue;
        string distributorAddress;

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        UnicastRoutingTable unicastRoutingTable;
        IDistributionPolicy defaultDistributionPolicy;
        string endpointName;

        public class State
        {
            public string ExplicitDestination { get; set; }
            public string SpecificInstance { get; set; }

            public RouteOption Option
            {
                get { return option; }
                set
                {
                    if (option != RouteOption.None)
                    {
                        throw new Exception("Already specified routing option for this message: " + option);
                    }
                    option = value;
                }
            }

            RouteOption option;
        }
    }
}