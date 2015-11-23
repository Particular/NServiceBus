namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Unicast;

    class BusContext : IBusContext
    {
        public BusContext(BehaviorContext context, 
            IPipeInlet<OutgoingSendContext> sendPipe,
            IPipeInlet<OutgoingPublishContext> publishPipe,
            IPipeInlet<SubscribeContext> subscribePipe,
            IPipeInlet<UnsubscribeContext> unsubscribePipe)
        {
            this.context = context;
            this.sendPipe = sendPipe;
            this.publishPipe = publishPipe;
            this.subscribePipe = subscribePipe;
            this.unsubscribePipe = unsubscribePipe;
            Extensions = context;
        }

        public ContextBag Extensions { get; }

        public Task Send(object message, SendOptions options)
        {
            return BusOperationsBehaviorContext.Send(sendPipe, context, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperationsBehaviorContext.Send(sendPipe, context, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return BusOperationsBehaviorContext.Publish(publishPipe, context, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperationsBehaviorContext.Publish(publishPipe, context, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            if (subscribePipe == null)
            {
                throw new InvalidOperationException("Subscribing is not allowed in context of a send-only endpoint.");
            }
            return BusOperationsBehaviorContext.Subscribe(subscribePipe, context, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            if (unsubscribePipe == null)
            {
                throw new InvalidOperationException("Unsubscribing is not allowed in context of a send-only endpoint.");
            }
            return BusOperationsBehaviorContext.Unsubscribe(unsubscribePipe, context, eventType, options);
        }

        BehaviorContext context;
        readonly IPipeInlet<OutgoingSendContext> sendPipe;
        readonly IPipeInlet<OutgoingPublishContext> publishPipe;
        readonly IPipeInlet<SubscribeContext> subscribePipe;
        readonly IPipeInlet<UnsubscribeContext> unsubscribePipe;
    }
}