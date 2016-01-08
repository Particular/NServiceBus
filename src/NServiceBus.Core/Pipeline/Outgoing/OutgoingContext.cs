namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;

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
            return BusOperations.Send(this, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperations.Send(this, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return BusOperations.Publish(this, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperations.Publish(this, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperations.Subscribe(this, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperations.Unsubscribe(this, eventType, options);
        }
    }
}