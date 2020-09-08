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

        public Task Send(object message, SendOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Send(context, message, options, cancellationToken);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Send(context, messageConstructor, options, cancellationToken);
        }

        public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Publish(context, message, options, cancellationToken);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken)
        {
            return messageOperations.Publish(context, messageConstructor, publishOptions, cancellationToken);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Subscribe(context, eventType, options, cancellationToken);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Unsubscribe(context, eventType, options, cancellationToken);
        }

        RootContext context;
        MessageOperations messageOperations;
    }
}