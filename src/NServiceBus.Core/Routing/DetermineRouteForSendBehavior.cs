namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using OutgoingPipeline;
    using Pipeline;
    using Routing;
    using TransportDispatch;
    using Unicast.Queuing;

    class DetermineRouteForSendBehavior : Behavior<OutgoingSendContext>
    {
        public DetermineRouteForSendBehavior(string localAddress, MessageRouter messageRouter, DynamicRoutingProvider dynamicRouting)
        {
            this.localAddress = localAddress;
            this.messageRouter = messageRouter;
            this.dynamicRouting = dynamicRouting;
        }

        public override async Task Invoke(OutgoingSendContext context, Func<Task> next)
        {
            var messageType = context.Message.MessageType;
            var destination = DetermineDestination(context, messageType);
            context.Set<RoutingStrategy>(new DirectToTargetDestination(destination));

            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Send.ToString());
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. You may have misconfigured the destination for this kind of message ({messageType}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex);
            }
        }

        string DetermineDestination(ContextBag context, Type messageType)
        {
            var state = context.GetOrCreate<State>();
            var destination = state.ExplicitDestination;

            if (string.IsNullOrEmpty(destination))
            {
                if (state.RouteToLocalInstance)
                {
                    destination = localAddress;
                }
                else
                {
                    if (!messageRouter.TryGetRoute(messageType, out destination))
                    {
                        throw new Exception("No destination specified for message: " + messageType);
                    }
                }
            }
            destination = dynamicRouting.GetRouteAddress(destination);
            return destination;
        }

        DynamicRoutingProvider dynamicRouting;
        string localAddress;
        MessageRouter messageRouter;

        public class State
        {
            public string ExplicitDestination { get; set; }
            public bool RouteToLocalInstance { get; set; }
        }
    }
}