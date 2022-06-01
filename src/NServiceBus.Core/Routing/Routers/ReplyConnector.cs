namespace NServiceBus
{
    using System;
    using System.Diagnostics;
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

            using var activity = ActivitySources.Main.StartActivity(ActivityNames.OutgoingMessageActivityName, ActivityKind.Producer);

            ActivityDecorator.SetReplyTags(activity, replyToAddress, context);
            ActivityDecorator.InjectHeaders(activity, context.Headers);

            context.Headers[Headers.MessageIntent] = MessageIntent.Reply.ToString();

            var addressLabels = new[] { new UnicastRoutingStrategy(replyToAddress) };
            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(context.Message, addressLabels, context);

            try
            {
                await stage(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException qnfe)
            {
                var err = new Exception($"The destination queue '{qnfe.Queue}' could not be found. " +
                                        "It may be the case that the given queue hasn't been created yet, or has been deleted.", qnfe);
                activity?.SetStatus(ActivityStatusCode.Error, err.Message);
                throw err;
            }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - enriching and rethrowing
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException

            //TODO should we stop the acitivty only once the message has been handed to the dispatcher?
            activity?.SetStatus(ActivityStatusCode.Ok); //Set acitivity state.
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