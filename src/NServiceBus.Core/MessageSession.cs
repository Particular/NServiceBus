namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class MessageSession : IMessageSession, IMessageSessionRaw
    {
        public MessageSession(RootContext context)
        {
            this.context = context;
        }

        public Task Send(object message, SendOptions options)
        {
            return MessageOperations.Send(context, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return MessageOperations.Send(context, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return MessageOperations.Publish(context, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return MessageOperations.Publish(context, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return MessageOperations.Subscribe(context, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return MessageOperations.Unsubscribe(context, eventType, options);
        }

        public Task Dispatch(params TransportOperation[] operations)
        {
            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IDispatchContext>();
            return pipeline.Invoke(new DispatchContext(operations, context));
        }

        RootContext context;
    }
}