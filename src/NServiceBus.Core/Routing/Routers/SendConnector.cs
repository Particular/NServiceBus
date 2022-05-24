namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Diagnostics;
    using Pipeline;
    using Unicast.Queuing;

    class SendConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        //TODO just a hack implementation to get the acceptance tests started
        const string OutgoingMessageActivityName = "NServiceBus.Diagnostics.OutgoingMessage";

        public SendConnector(UnicastSendRouter unicastSendRouter)
        {
            this.unicastSendRouter = unicastSendRouter;
        }

        public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var activity = ActivitySources.Main.StartActivity(OutgoingMessageActivityName, ActivityKind.Producer);
            activity?.SetTag("NServiceBus.MessageId", context.MessageId);

            if (activity != null)
            {
                context.Headers.Add("traceparent", activity.Id);
            }

            var routingStrategy = unicastSendRouter.Route(context);
            context.Headers[Headers.MessageIntent] = MessageIntent.Send.ToString();
            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(context.Message, new[] { routingStrategy }, context);

            try
            {
                await stage(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({context.Message.MessageType}) in the routing section of the transport configuration. It may also be the case that the given queue hasn't been created yet, or has been deleted.", ex);
            }

            activity?.Dispose(); //TODO ensure disposal. Set acitivity state.
        }

        readonly UnicastSendRouter unicastSendRouter;
    }
}