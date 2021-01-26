namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class MessageSession : IMessageSession
    {
        public MessageSession(RootContext context)
        {
            this.context = context;
            messageOperations = context.Get<MessageOperations>();
        }

        public Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);
            return messageOperations.Send(context, message, sendOptions, cancellationToken);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);
            return messageOperations.Send(context, messageConstructor, sendOptions, cancellationToken);
        }

        public Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);
            return messageOperations.Publish(context, message, publishOptions, cancellationToken);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);
            return messageOperations.Publish(context, messageConstructor, publishOptions, cancellationToken);
        }

        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(subscribeOptions), subscribeOptions);
            return messageOperations.Subscribe(context, eventType, subscribeOptions, cancellationToken);
        }

        public Task SubscribeAll(Type[] eventTypes, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
        {
            // set a flag on the context so that subscribe implementations know which send API was used.
            subscribeOptions.Context.Set(SubscribeAllFlagKey, true);
            return messageOperations.Subscribe(context, eventTypes, subscribeOptions, cancellationToken);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(unsubscribeOptions), unsubscribeOptions);
            return messageOperations.Unsubscribe(context, eventType, unsubscribeOptions, cancellationToken);
        }

        RootContext context;
        MessageOperations messageOperations;

        internal const string SubscribeAllFlagKey = "NServiceBus.SubscribeAllFlag";
    }
}