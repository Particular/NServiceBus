namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

        public string MessageId { get; }

        public Dictionary<string, string> Headers { get; }

        public Task Send(object message, SendOptions options)
        {
            return MessageOperations.Send(this, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return MessageOperations.Send(this, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return MessageOperations.Publish(this, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return MessageOperations.Publish(this, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return MessageOperations.Subscribe(this, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return MessageOperations.Unsubscribe(this, eventType, options);
        }
    }
}