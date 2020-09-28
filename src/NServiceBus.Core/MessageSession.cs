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

        public Task Send(object message, SendOptions sendOptions)
        {
            return messageOperations.Send(context, message, sendOptions);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions)
        {
            return messageOperations.Send(context, messageConstructor, sendOptions);
        }

        public Task Publish(object message, PublishOptions publishOptions)
        {
            return messageOperations.Publish(context, message, publishOptions);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return messageOperations.Publish(context, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions)
        {
            return messageOperations.Subscribe(context, eventType, subscribeOptions);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions)
        {
            return messageOperations.Unsubscribe(context, eventType, unsubscribeOptions);
        }

        RootContext context;
        MessageOperations messageOperations;
    }
}