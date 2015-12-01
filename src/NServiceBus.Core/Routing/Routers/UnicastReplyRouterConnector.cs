namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Outgoing;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Unicast.Queuing;
    using OutgoingPipeline;
    using Pipeline;
    using Routing;
    using Transports;

    class UnicastReplyRouterConnector : StageConnector<OutgoingReplyContext, OutgoingLogicalMessageContext>
    {
        public override async Task Invoke(OutgoingReplyContext context, Func<OutgoingLogicalMessageContext, Task> next)
        {
            var state = context.Extensions.GetOrCreate<State>();

            var replyToAddress = state.ExplicitDestination;

            if (string.IsNullOrEmpty(replyToAddress))
            {
                replyToAddress = GetReplyToAddressFromIncomingMessage(context);
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Reply.ToString();

            var addressLabels = RouteToDestination(replyToAddress).EnsureNonEmpty(() => "No destination specified.").ToArray();
            var logicalMessageContext = new OutgoingLogicalMessageContextImpl(
                    context.MessageId,
                    context.Headers,
                    context.Message,
                    addressLabels,
                    context);

            try
            {
                await next(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. It may be the case that the given queue just hasn't been created yet, or has been deleted.", ex);
            }
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
                throw new Exception($"No `ReplyToAddress` found on the {context.Message.MessageType.FullName} being processed");
            }

            return replyToAddress;
        }

        static IEnumerable<UnicastRoutingStrategy> RouteToDestination(string physicalAddress)
        {
            yield return new UnicastRoutingStrategy(physicalAddress);
        }

        public class State
        {
            public string ExplicitDestination { get; set; }
        }
    }
}