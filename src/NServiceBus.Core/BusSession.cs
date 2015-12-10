namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class BusSession : IBusSession
    {
        public BusSession(RootContext context)
        {
            this.context = context;
        }

        public Task Send(object message, SendOptions options)
        {
            return BusOperations.Send(context, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperations.Send(context, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return BusOperations.Publish(context, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperations.Publish(context, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperations.Subscribe(context, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperations.Unsubscribe(context, eventType, options);
        }

        RootContext context;
    }
}