namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Unicast.Queuing;

    class DetermineRouteForSendBehavior : Behavior<OutgoingSendContext>
    {
        string localAddress;
        MessageRouter messageRouter;

        public DetermineRouteForSendBehavior(string localAddress, MessageRouter messageRouter)
        {
            this.localAddress = localAddress;
            this.messageRouter = messageRouter;
        }

        public override async Task Invoke(OutgoingSendContext context, Func<Task> next)
        {
            var messageType = context.GetMessageType();
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
            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Send.ToString());

            context.Set<RoutingStrategy>(new DirectToTargetDestination(destination));

            try
            {
                await next().ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception(string.Format("The destination queue '{0}' could not be found. You may have misconfigured the destination for this kind of message ({1}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex.Queue, messageType), ex);
            }
        }

        public class State
        {
            public string ExplicitDestination { get; set; }
            public bool RouteToLocalInstance { get; set; }
        }
    }
}