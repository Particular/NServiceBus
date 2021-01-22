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
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);
            return messageOperations.Send(context, message, sendOptions);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);
            return messageOperations.Send(context, messageConstructor, sendOptions);
        }

        public Task Publish(object message, PublishOptions publishOptions)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);
            return messageOperations.Publish(context, message, publishOptions);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);
            return messageOperations.Publish(context, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(subscribeOptions), subscribeOptions);
            return messageOperations.Subscribe(context, eventType, subscribeOptions);
        }

        public Task SubscribeAll(Type[] eventTypes, SubscribeOptions subscribeOptions)
        {
            // set a flag on the context so that subscribe implementations know which send API was used.
            subscribeOptions.Context.Set(SubscribeAllFlagKey, true);
            return messageOperations.Subscribe(context, eventTypes, subscribeOptions);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(unsubscribeOptions), unsubscribeOptions);
            return messageOperations.Unsubscribe(context, eventType, unsubscribeOptions);
        }

        RootContext context;
        MessageOperations messageOperations;

        internal const string SubscribeAllFlagKey = "NServiceBus.SubscribeAllFlag";
    }
}