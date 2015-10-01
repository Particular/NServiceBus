namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Transports;
    using OutgoingPipeline;
    using Pipeline;
    using Routing;
    using TransportDispatch;

    class DetermineRouteForReplyBehavior : Behavior<OutgoingReplyContext>
    {
        public override Task Invoke(OutgoingReplyContext context, Func<Task> next)
        {
            var state = context.GetOrCreate<State>();

            var replyToAddress = state.ExplicitDestination;

            if (string.IsNullOrEmpty(replyToAddress))
            {
                replyToAddress = GetReplyToAddressFromIncomingMessage(context);
            }

            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Reply.ToString());

            context.Set<RoutingStrategy>(new DirectToTargetDestination(replyToAddress));

            return next();
        }

        static string GetReplyToAddressFromIncomingMessage(OutgoingReplyContext context)
        {
            IncomingMessage incomingMessage;

            if (!context.TryGetIncomingPhysicalMessage(out incomingMessage))
            {
                throw new Exception("No incoming message found, replies are only valid to call from a message handler");
            }

            string replyToAddress;

            if (!incomingMessage.Headers.TryGetValue(Headers.ReplyToAddress, out replyToAddress))
            {
                throw new Exception($"No `ReplyToAddress` found on the {context.GetMessageType().FullName} being processed");
            }

            return replyToAddress;
        }

        public class State
        {
            public string ExplicitDestination { get; set; }
        }
    }
}