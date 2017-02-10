namespace NServiceBus
{
    using System;
    using Pipeline;
    using Routing;
    using Transport;

    static partial class UnicastSend
    {
        public class PhysicalRouter
        {
            public PhysicalRouter(string instanceSpecificQueue, string distributorAddress)
            {
                this.distributorAddress = distributorAddress;
                this.instanceSpecificQueue = instanceSpecificQueue;
            }

            public virtual UnicastRoutingStrategy Route(IOutgoingSendContext context)
            {
                var state = context.Extensions.GetOrCreate<State>();

                if (state.Option == RouteOption.RouteToThisInstance && instanceSpecificQueue == null)
                {
                    throw new InvalidOperationException("Cannot route to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
                }


                var distributor = state.Option == RouteOption.RouteToAnyInstanceOfThisEndpoint && IsMessageWithWorkerSession(context) ? distributorAddress : null;
                var thisInstance = state.Option == RouteOption.RouteToThisInstance ? instanceSpecificQueue : null;
                var explicitDestination = state.Option == RouteOption.ExplicitDestination ? state.ExplicitDestination : null;
                var destination = explicitDestination ?? thisInstance ?? distributor;

                return destination != null ? new UnicastRoutingStrategy(destination) : null;
            }

            bool IsMessageWithWorkerSession(IOutgoingSendContext context)
            {
                IncomingMessage incomingMessage;
                return distributorAddress != null && context.Extensions.TryGet(out incomingMessage) && incomingMessage.Headers.ContainsKey(LegacyDistributorHeaders.WorkerSessionId);
            }

            string instanceSpecificQueue;
            string distributorAddress;
        }
    }
}