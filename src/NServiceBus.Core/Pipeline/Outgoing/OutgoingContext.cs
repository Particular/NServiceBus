namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    abstract class OutgoingContext : BehaviorContext, IOutgoingContext
    {
        protected OutgoingContext(string messageId, Dictionary<string, string> headers, IBehaviorContext parentContext)
            : base(parentContext)
        {
            Headers = headers;
            MessageId = messageId;
        }

        MessageOperations messageOperations => Extensions.Get<MessageOperations>();

        public string MessageId { get; }

        public Dictionary<string, string> Headers { get; }

        public Task Send(object message, SendOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Send(this, message, options, cancellationToken);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Send(this, messageConstructor, options, cancellationToken);
        }

        public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Publish(this, message, options, cancellationToken);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken)
        {
            return messageOperations.Publish(this, messageConstructor, publishOptions, cancellationToken);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Subscribe(this, eventType, options, cancellationToken);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken)
        {
            return messageOperations.Unsubscribe(this, eventType, options, cancellationToken);
        }
    }
}