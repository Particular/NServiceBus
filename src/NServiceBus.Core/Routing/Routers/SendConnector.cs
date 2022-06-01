namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Pipeline;
    using Unicast.Queuing;

    class SendConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        public SendConnector(UnicastSendRouter unicastSendRouter)
        {
            this.unicastSendRouter = unicastSendRouter;
        }

        public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            using var activity = ActivitySources.Main.StartActivity(ActivityNames.OutgoingMessageActivityName, ActivityKind.Producer);

            ActivityDecorator.SetSendTags(activity, context);
            ActivityDecorator.InjectHeaders(activity, context.Headers);

            var routingStrategy = unicastSendRouter.Route(context);
            context.Headers[Headers.MessageIntent] = MessageIntent.Send.ToString();
            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(context.Message, new[] { routingStrategy }, context);

            try
            {
                await stage(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException qnfe)
            {
                var err = new Exception($"The destination queue '{qnfe.Queue}' could not be found. " +
                                        $"The destination may be misconfigured for this kind of message ({context.Message.MessageType}) in the routing section of the transport configuration. It may also be the case that the given queue hasn't been created yet, or has been deleted.", qnfe);
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

        readonly UnicastSendRouter unicastSendRouter;
    }
}