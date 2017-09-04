namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Unicast.Queuing;

    class UnicastReplyRouterConnector : StageConnector<IOutgoingReplyContext, IOutgoingLogicalMessageContext>
    {
        public override async Task Invoke(IOutgoingReplyContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var state = context.Extensions.GetOrCreate<State>();

            var replyToAddress = state.ExplicitDestination;

            if (string.IsNullOrEmpty(replyToAddress))
            {
                replyToAddress = GetReplyToAddressFromIncomingMessage(context);
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Reply.ToString();

            var addressLabels = new []{ new UnicastRoutingStrategy(replyToAddress) };
            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(context.Message, addressLabels, context);

            try
            {
                await stage(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. It may be the case that the given queue hasn't been created yet, or has been deleted.", ex);
            }
        }

        static string GetReplyToAddressFromIncomingMessage(IOutgoingReplyContext context)
        {
            if (!context.TryGetIncomingPhysicalMessage(out var incomingMessage))
            {
                throw new Exception("No incoming message found, replies are only valid to call from a message handler");
            }

            if (!incomingMessage.Headers.TryGetValue(Headers.ReplyToAddress, out var replyToAddress))
            {
                throw new Exception($"No `ReplyToAddress` found on the {context.Message.MessageType.FullName} being processed");
            }

            return replyToAddress;
        }

        public class State
        {
            public string ExplicitDestination { get; set; }
        }
    }
}