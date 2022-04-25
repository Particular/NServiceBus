namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;
    using Routing;
    using Unicast.Queuing;

    class ReplyConnector : StageConnector<IOutgoingReplyContext, IOutgoingLogicalMessageContext>
    {
        public override async Task Invoke(IOutgoingReplyContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            string replyToAddress = null;
            if (context.GetOperationProperties().TryGet(out State state))
            {
                replyToAddress = state.ExplicitDestination;
            }

            if (string.IsNullOrEmpty(replyToAddress))
            {
                replyToAddress = GetReplyToAddressFromIncomingMessage(context);
            }

            context.Headers[Headers.MessageIntent] = MessageIntent.Reply.ToString();

            var addressLabels = new[] { new UnicastRoutingStrategy(replyToAddress) };
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