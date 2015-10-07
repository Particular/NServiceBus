namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Unicast;

    class BusContext : IBusContext
    {
        public BusContext(BehaviorContext context, BusOperations busOperations)
        {
            this.context = context;
            Extensions = context;
            BusOperations = busOperations;
        }

        protected BusOperations BusOperations { get; }

        public ContextBag Extensions { get; }

        public Task SendAsync(object message, SendOptions options)
        {
            return BusOperations.SendAsync(message, options, context);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperations.SendAsync(messageConstructor, options, context);
        }

        public Task PublishAsync(object message, PublishOptions options)
        {
            return BusOperations.PublishAsync(message, options, context);
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperations.PublishAsync(messageConstructor, publishOptions, context);
        }

        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            return BusOperations.SubscribeAsync(eventType, options, context);
        }

        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            return BusOperations.UnsubscribeAsync(eventType, options, context);
        }

        BehaviorContext context;
    }
}