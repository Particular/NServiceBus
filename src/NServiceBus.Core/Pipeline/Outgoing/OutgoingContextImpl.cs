namespace NServiceBus.OutgoingPipeline
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Unicast;
    using PublishOptions = NServiceBus.PublishOptions;
    using SendOptions = NServiceBus.SendOptions;

    abstract class OutgoingContextImpl : BehaviorContextImpl, OutgoingContext
    {
        protected OutgoingContextImpl(string messageId, Dictionary<string, string> headers, BehaviorContext parentContext)
            : base(parentContext)
        {
            Headers = headers;
            MessageId = messageId;
        }

        public string MessageId { get; }

        public Dictionary<string, string> Headers { get; }

        /// <inheritdoc/>
        public Task Send(object message, SendOptions options)
        {
            return BusOperationsBehaviorContext.Send(this, message, options);
        }

        /// <inheritdoc/>
        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperationsBehaviorContext.Send(this, messageConstructor, options);
        }

        /// <inheritdoc/>
        public Task Publish(object message, PublishOptions options)
        {
            return BusOperationsBehaviorContext.Publish(this, message, options);
        }

        /// <inheritdoc/>
        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperationsBehaviorContext.Publish(this, messageConstructor, publishOptions);
        }

        /// <inheritdoc/>
        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperationsBehaviorContext.Subscribe(this, eventType, options);
        }

        /// <inheritdoc/>
        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperationsBehaviorContext.Unsubscribe(this, eventType, options);
        }
    }
}