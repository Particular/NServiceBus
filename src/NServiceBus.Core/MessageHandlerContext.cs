namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Unicast;

    class MessageHandlerContext : BusContext, IMessageHandlerContext
    {
        readonly InvokeHandlerContext context;
        readonly IPipeInlet<OutgoingReplyContext> replyPipe;
        readonly IPipeInlet<RoutingContext> routingPipe;

        public MessageHandlerContext(
            InvokeHandlerContext context,
            IPipeInlet<OutgoingSendContext> sendPipe,
            IPipeInlet<OutgoingPublishContext> publishPipe,
            IPipeInlet<OutgoingReplyContext> replyPipe,
            IPipeInlet<RoutingContext> routingPipe,
            IPipeInlet<SubscribeContext> subscribePipe,
            IPipeInlet<UnsubscribeContext> unsubscribePipe)
            : base(context, sendPipe, publishPipe, subscribePipe, unsubscribePipe)
        {
            this.context = context;
            this.replyPipe = replyPipe;
            this.routingPipe = routingPipe;
        }

        public string MessageId => context.MessageId;
        public string ReplyToAddress => context.ReplyToAddress;
        public IReadOnlyDictionary<string, string> MessageHeaders => context.MessageHeaders;

        public Task Reply(object message, ReplyOptions options)
        {
            return BusOperationsBehaviorContext.Reply(replyPipe, context, message, options);
        }

        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return BusOperationsBehaviorContext.Reply(replyPipe, context, messageConstructor, options);
        }

        public Task ForwardCurrentMessageTo(string destination)
        {
            return BusOperationsIncomingContext.ForwardCurrentMessageTo(routingPipe, context, destination);
        }

        public Task HandleCurrentMessageLater()
        {
            return BusOperationsInvokeHandlerContext.HandleCurrentMessageLater(routingPipe, context);
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            context.DoNotContinueDispatchingCurrentMessageToHandlers();
        }
    }
}