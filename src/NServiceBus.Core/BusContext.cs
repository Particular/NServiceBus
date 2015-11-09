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

        public Task SendAsync(object message, SendOptions options)
        {
            return BusOperationsBehaviorContext.SendAsync(context, message, options);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperationsBehaviorContext.SendAsync(context, messageConstructor, options);
        }

        public Task PublishAsync(object message, PublishOptions options)
        {
            return BusOperationsBehaviorContext.PublishAsync(context, message, options);
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperationsBehaviorContext.PublishAsync(context, messageConstructor, publishOptions);
        }

        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            return BusOperationsBehaviorContext.SubscribeAsync(context, eventType, options);
        }

        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            return BusOperationsBehaviorContext.UnsubscribeAsync(context, eventType, options);
        }

        BehaviorContext context;
    }
}