namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Queuing;
    using OutgoingPipeline;
    using Pipeline;
    using Routing;
    using TransportDispatch;

    class DirectReplyRouterConnector : StageConnector<OutgoingReplyContext, OutgoingLogicalMessageContext>
    {
        public override async Task Invoke(OutgoingReplyContext context, Func<OutgoingLogicalMessageContext, Task> next)
        {
            var state = context.GetOrCreate<State>();

            var replyToAddress = state.ExplicitDestination;

            if (string.IsNullOrEmpty(replyToAddress))
            {
                replyToAddress = GetReplyToAddressFromIncomingMessage(context);
            }

            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Reply.ToString());

            var addressLabels = RouteToDestination(replyToAddress).EnsureNonEmpty(() => "No destination specified.").ToArray();

            try
            {
                await next(new OutgoingLogicalMessageContext(context.Message, addressLabels, context)).ConfigureAwait(false);
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

        static IEnumerable<AddressLabel> RouteToDestination(string physicalAddress)
        {
            yield return new DirectAddressLabel(physicalAddress);
        }

        public class State
        {
            public string ExplicitDestination { get; set; }
        }
    }
}