namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class MessageSession : IMessageSession
    {
        public MessageSession(RootContext context)
        {
            this.context = context;
            messageOperations = context.Get<MessageOperations>();
        }

        public Task Send(object message, SendOptions options)
        {
            return messageOperations.Send(context, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return messageOperations.Send(context, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return messageOperations.Publish(context, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return messageOperations.Publish(context, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return messageOperations.Subscribe(context, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return messageOperations.Unsubscribe(context, eventType, options);
        }

        RootContext context;
        MessageOperations messageOperations;
    }
}