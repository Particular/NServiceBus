namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Unicast.Queuing;

    enum UnicastRouteOption
    {
        None,
        ExplicitDestination,
        RouteToThisInstance,
        RouteToAnyInstanceOfThisEndpoint,
        RouteToSpecificInstance
    }

    class UnicastSendRouterConnector : StageConnector<IOutgoingSendContext, IOutgoingDistributionContext>
    {
        public UnicastSendRouterConnector(
            string sharedQueue,
            string instanceSpecificQueue,
            UnicastRoutingTable unicastRoutingTable)
        {
            this.sharedQueue = sharedQueue;
            this.instanceSpecificQueue = instanceSpecificQueue;
            this.unicastRoutingTable = unicastRoutingTable;
        }

        public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingDistributionContext, Task> stage)
        {
            var messageType = context.Message.MessageType;

            var state = context.Extensions.GetOrCreate<State>();

            if (state.Option == UnicastRouteOption.RouteToThisInstance && instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
            }
            var thisEndpoint = state.Option == UnicastRouteOption.RouteToAnyInstanceOfThisEndpoint ? sharedQueue : null;
            var thisInstance = state.Option == UnicastRouteOption.RouteToThisInstance ? instanceSpecificQueue : null;
            var explicitDestination = state.Option == UnicastRouteOption.ExplicitDestination ? state.ExplicitDestination : null;
            var destination = explicitDestination ?? thisInstance ?? thisEndpoint;

            var route = string.IsNullOrEmpty(destination)
                ? unicastRoutingTable.GetRouteFor(messageType)
                : UnicastRoute.CreateFromPhysicalAddress(destination);

            if (route == null)
            {
                throw new Exception($"No destination specified for message: {messageType}");
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Send.ToString();

            var downstreamContext = this.CreateDistributionContext(
                context.Message,
                new[]
                {
                    route
                },
                context);

            try
            {
                await stage(downstreamContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({messageType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn't been created yet, or has been deleted.", ex);
            }
        }

        string instanceSpecificQueue;
        UnicastRoutingTable unicastRoutingTable;
        string sharedQueue;

        public class State
        {
            public string ExplicitDestination { get; set; }
            public string SpecificInstance { get; set; }

            public UnicastRouteOption Option
            {
                get { return option; }
                set
                {
                    if (option != UnicastRouteOption.None)
                    {
                        throw new Exception("Already specified routing option for this message: " + option);
                    }
                    option = value;
                }
            }

            UnicastRouteOption option;
        }
    }
}