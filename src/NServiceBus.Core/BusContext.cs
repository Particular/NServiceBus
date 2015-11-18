namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Unicast;

    class BusContext : IBusContext
    {
        public BusContext(BehaviorContext context)
        {
            this.context = context;
            Extensions = context;
        }

        public ContextBag Extensions { get; }

        public Task Send(object message, SendOptions options)
        {
            return BusOperationsBehaviorContext.SendAsync(context, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperationsBehaviorContext.SendAsync(context, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return BusOperationsBehaviorContext.PublishAsync(context, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperationsBehaviorContext.PublishAsync(context, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperationsBehaviorContext.SubscribeAsync(context, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperationsBehaviorContext.UnsubscribeAsync(context, eventType, options);
        }

        BehaviorContext context;
    }
}