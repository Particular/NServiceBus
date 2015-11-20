namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using Pipeline.Contexts;
    using Sagas;

    class InvokeSagaNotFoundBehavior : Behavior<LogicalMessageProcessingContext>
    {
        static ILog logger = LogManager.GetLogger<InvokeSagaNotFoundBehavior>();

        IPipeInlet<OutgoingSendContext> sendPipe;
        IPipeInlet<OutgoingPublishContext> publishPipe;
        IPipeInlet<OutgoingReplyContext> replyPipe;
        IPipeInlet<RoutingContext> routingPipe;
        IPipeInlet<SubscribeContext> subscribePipe;
        IPipeInlet<UnsubscribeContext> unsubscribePipe;

        public InvokeSagaNotFoundBehavior(
            IPipeInlet<OutgoingSendContext> sendPipe,
            IPipeInlet<OutgoingPublishContext> publishPipe,
            IPipeInlet<OutgoingReplyContext> replyPipe,
            IPipeInlet<RoutingContext> routingPipe,
            IPipeInlet<SubscribeContext> subscribePipe,
            IPipeInlet<UnsubscribeContext> unsubscribePipe)
        {
            this.sendPipe = sendPipe;
            this.publishPipe = publishPipe;
            this.replyPipe = replyPipe;
            this.routingPipe = routingPipe;
            this.subscribePipe = subscribePipe;
            this.unsubscribePipe = unsubscribePipe;
        }

        public override async Task Invoke(LogicalMessageProcessingContext context, Func<Task> next)
        {
            var invocationResult = new SagaInvocationResult();
            context.Set(invocationResult);

            await next().ConfigureAwait(false);

            if (invocationResult.WasFound)
            {
                return;    
            }

            logger.InfoFormat("Could not find a started saga for '{0}' message type. Going to invoke SagaNotFoundHandlers.", context.Message.MessageType.FullName);

            foreach (var handler in context.Builder.BuildAll<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking SagaNotFoundHandler ('{0}')", handler.GetType().FullName);
                var processingContext = new MessageProcessingContext(context,sendPipe, publishPipe, replyPipe, routingPipe, subscribePipe, unsubscribePipe);
                await handler.Handle(context.Message.Instance, processingContext).ConfigureAwait(false);
            }
        }
    }
}