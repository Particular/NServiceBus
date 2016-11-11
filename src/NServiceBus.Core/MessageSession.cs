namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class MessageSession : IMessageSession
    {
        public MessageSession(RootContext context)
        {
            this.context = context;
        }

        public Task Send(object message, SendOptions options)
        {
            return MessageOperations.Send(context, message, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return MessageOperations.Publish(context, message, options);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return MessageOperations.Subscribe(context, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return MessageOperations.Unsubscribe(context, eventType, options);
        }

        RootContext context;
    }
}