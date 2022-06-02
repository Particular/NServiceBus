namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;
    using Routing;

    class ReplyConnector : StageConnector<IOutgoingReplyContext, IOutgoingLogicalMessageContext>
    {
        public override Task Invoke(IOutgoingReplyContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
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

            using var activity = ActivitySources.Main.StartActivity(ActivityNames.OutgoingMessageActivityName, ActivityKind.Producer);

            ActivityDecorator.SetReplyTags(activity, replyToAddress, context);
            ActivityDecorator.InjectHeaders(activity, context.Headers);

            context.Headers[Headers.MessageIntent] = MessageIntent.Reply.ToString();

            var addressLabels = new[] { new UnicastRoutingStrategy(replyToAddress) };
            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(context.Message, addressLabels, context);

            return LogicalMessageStager.StageOutgoing(stage, logicalMessageContext, activity);
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