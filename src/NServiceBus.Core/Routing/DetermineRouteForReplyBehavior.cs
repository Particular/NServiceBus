namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;

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
            TransportMessage incomingMessage;

            if (!context.TryGetIncomingPhysicalMessage(out incomingMessage))
            {
                throw new Exception("No incoming message found, replies are only valid to call from a message handler");
            }

            string replyToAddress;

            if (!incomingMessage.Headers.TryGetValue(Headers.ReplyToAddress, out replyToAddress))
            {
                throw new Exception(string.Format("No `ReplyToAddress` found on the {0} being processed", context.GetMessageType().FullName));
            }

            return replyToAddress;
        }

        public class State
        {
            public string ExplicitDestination { get; set; }
        }
    }
}