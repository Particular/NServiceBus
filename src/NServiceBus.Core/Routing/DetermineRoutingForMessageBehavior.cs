namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Unicast.Queuing;

    class DetermineRoutingForMessageBehavior : Behavior<OutgoingContext>
    {
        readonly string localAddress;
        readonly MessageRouter messageRouter;

        public DetermineRoutingForMessageBehavior(string localAddress, MessageRouter messageRouter)
        {
            this.localAddress = localAddress;
            this.messageRouter = messageRouter;
        }

        public override void Invoke(OutgoingContext context, Action next)
        {
            RoutingStrategy routingStrategy = null;

            var intent = MessageIntentEnum.Send;

            if (context.IsSend())
            {
                var state = context.Extensions.GetOrCreate<State>();

                var destination = state.ExplicitDestination;

                if (string.IsNullOrEmpty(destination))
                {
                    if (state.RouteToLocalInstance)
                    {
                        destination = localAddress;
                    }
                    else
                    {
                        if (!messageRouter.TryGetRoute(context.MessageType, out destination))
                        {
                            throw new InvalidOperationException("No destination specified for message: " + context.MessageType);
                        }
                    }
                }

                routingStrategy = new DirectToTargetDestination(destination);
                intent = MessageIntentEnum.Send;

            }
            if (context.IsPublish())
            {
                routingStrategy = new ToAllSubscribers(context.MessageType);
                intent = MessageIntentEnum.Publish;
            }

            if (context.IsReply())
            {
                var state = context.Extensions.GetOrCreate<State>();

                var replyToAddress = state.ExplicitDestination;

                if (string.IsNullOrEmpty(replyToAddress))
                {
                    replyToAddress = GetReplyToAddressFromIncomingMessage(context);
                }

                routingStrategy = new DirectToTargetDestination(replyToAddress);

                intent = MessageIntentEnum.Reply;
            }

            if (routingStrategy == null)
            {
                throw new Exception("No routing strategy could be determined for message");
            }

            context.SetHeader(Headers.MessageIntent, intent.ToString());

            context.Set(routingStrategy);

            try
            {
                next();
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception(string.Format("The destination queue '{0}' could not be found. You may have misconfigured the destination for this kind of message ({1}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex.Queue, context.MessageType), ex);
            }
        }

        static string GetReplyToAddressFromIncomingMessage(OutgoingContext context)
        {
            TransportMessage incomingMessage;

            if (!context.TryGetIncomingPhysicalMessage(out incomingMessage))
            {
                throw new Exception("No incoming message found, replies are only valid to call from a message handler");
            }

            string replyToAddress;

            if (!incomingMessage.Headers.TryGetValue(Headers.ReplyToAddress, out replyToAddress))
            {
                throw new Exception("No `ReplyToAddress` found on the message being processed");
            }
            return replyToAddress;
        }

        public class State
        {
            public string ExplicitDestination { get; set; }
            public bool RouteToLocalInstance { get; set; }
        }
    }
}