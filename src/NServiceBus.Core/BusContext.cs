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
            Bus = context.Builder.Build<StaticBus>();
            Extensions = context;
        }

        protected StaticBus Bus { get; }

        public ContextBag Extensions { get; }

        public Task SendAsync(object message, SendOptions options)
        {
            return Bus.SendAsync(message, options, context);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return Bus.SendAsync(messageConstructor, options, context);
        }

        public Task PublishAsync(object message, PublishOptions options)
        {
            return Bus.PublishAsync(message, options, context);
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return Bus.PublishAsync(messageConstructor, publishOptions, context);
        }

        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            return Bus.SubscribeAsync(eventType, options, context);
        }

        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            return Bus.UnsubscribeAsync(eventType, options, context);
        }

        BehaviorContext context;
    }
}