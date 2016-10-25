namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    class MulticastSendRouterBehavior : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        string instanceSpecificQueue;
        string sharedQueue;

        public MulticastSendRouterBehavior(string sharedQueue, string instanceSpecificQueue)
        {
            this.sharedQueue = sharedQueue;
            this.instanceSpecificQueue = instanceSpecificQueue;
        }

        public override Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var state = context.Extensions.GetOrCreate<SendRouterConnectorArguments>();

            if (state.Option == SendRouteOption.RouteToThisInstance && instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
            }
            var thisEndpoint = state.Option == SendRouteOption.RouteToAnyInstanceOfThisEndpoint ? sharedQueue : null;
            var thisInstance = state.Option == SendRouteOption.RouteToThisInstance ? instanceSpecificQueue : null;
            var explicitDestination = state.Option == SendRouteOption.ExplicitDestination ? state.ExplicitDestination : null;
            var destination = explicitDestination ?? thisInstance ?? thisEndpoint;

            RoutingStrategy routingStrategy;
            if (string.IsNullOrEmpty(destination))
            {
                routingStrategy = new MulticastRoutingStrategy(context.Message.MessageType);
            }
            else
            {
                routingStrategy = new UnicastRoutingStrategy(destination);
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Send.ToString();

            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(
                context.Message,
                new[]
                {
                    routingStrategy
                },
                context);

            return stage(logicalMessageContext);
        }
    }
}