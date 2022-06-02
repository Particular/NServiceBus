namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Pipeline;

    class SendConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        public SendConnector(UnicastSendRouter unicastSendRouter)
        {
            this.unicastSendRouter = unicastSendRouter;
        }

        public override Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            using var activity = ActivitySources.Main.StartActivity(ActivityNames.OutgoingMessageActivityName, ActivityKind.Producer);

            ActivityDecorator.SetSendTags(activity, context);
            ActivityDecorator.InjectHeaders(activity, context.Headers);

            var routingStrategy = unicastSendRouter.Route(context);
            context.Headers[Headers.MessageIntent] = MessageIntent.Send.ToString();
            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(context.Message, new[] { routingStrategy }, context);

            return LogicalMessageStager.StageOutgoing(stage, logicalMessageContext, activity,
                $"The destination may be misconfigured for this kind of message ({context.Message.MessageType}) in the routing section of the transport configuration. ");
        }

        readonly UnicastSendRouter unicastSendRouter;
    }
}