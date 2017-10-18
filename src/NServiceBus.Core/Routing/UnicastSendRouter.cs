namespace NServiceBus
{
    using System;
    using System.Linq;
    using Pipeline;
    using Routing;

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
            bool isSendOnly,
            string receiveQueueName,
            string instanceSpecificQueue,
            IDistributionPolicy defaultDistributionPolicy,
            UnicastRoutingTable unicastRoutingTable,
            EndpointInstances endpointInstances,
            Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.isSendOnly = isSendOnly;
            this.receiveQueueName = receiveQueueName;
            this.instanceSpecificQueue = instanceSpecificQueue;
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
                    return RouteToAnyInstance();
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
            if (isSendOnly)
            {
                throw new InvalidOperationException("Cannot route to this instance since the endpoint is configured to be in send-only mode.");
            }

            if (instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
            }

            return UnicastRoute.CreateFromPhysicalAddress(instanceSpecificQueue);
        }

        UnicastRoute RouteToAnyInstance()
        {
            if (isSendOnly)
            {
                throw new InvalidOperationException("Cannot route to instances of this endpoint since it's configured to be in send-only mode.");
            }

            return UnicastRoute.CreateFromEndpointName(receiveQueueName);
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

        UnicastRoute RouteUsingTable(IOutgoingSendContext context)
        {
            var route = unicastRoutingTable.GetRouteFor(context.Message.MessageType);
            if (route == null)
            {
                throw new Exception($"No destination specified for message: {context.Message.MessageType}");
            }
            return route;
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

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        UnicastRoutingTable unicastRoutingTable;
        IDistributionPolicy defaultDistributionPolicy;
        readonly bool isSendOnly;
        string receiveQueueName;

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