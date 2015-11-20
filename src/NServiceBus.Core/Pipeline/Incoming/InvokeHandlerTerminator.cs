namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using Pipeline;
    using Pipeline.Contexts;
    using Sagas;

    class InvokeHandlerTerminator : PipelineTerminator<InvokeHandlerContext>
    {
        IPipeInlet<OutgoingSendContext> sendPipe;
        IPipeInlet<OutgoingPublishContext> publishPipe;
        IPipeInlet<OutgoingReplyContext> replyPipe;
        IPipeInlet<RoutingContext> routingPipe;
        IPipeInlet<SubscribeContext> subscribePipe;
        IPipeInlet<UnsubscribeContext> unsubscribePipe;

        public InvokeHandlerTerminator(IPipeInlet<OutgoingSendContext> sendPipe,
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

        protected override async Task Terminate(InvokeHandlerContext context)
        {
            context.Set(new State { ScopeWasPresent = Transaction.Current != null });

            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
            {
                return;
            }

            var messageHandler = context.MessageHandler;
            await messageHandler
                .Invoke(context.MessageBeingHandled, new MessageHandlerContext(context,sendPipe, publishPipe, replyPipe, routingPipe, subscribePipe, unsubscribePipe))
                .ConfigureAwait(false);
        }

        public class State
        {
            public bool ScopeWasPresent { get; set; }
        }
    }
}