namespace NServiceBus.OutgoingPipeline
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Unicast;
    using PublishOptions = NServiceBus.PublishOptions;
    using SendOptions = NServiceBus.SendOptions;

    /// <summary>
    /// 
    /// </summary>
    public interface OutgoingContext : BehaviorContext, IBusContext
    {
        /// <summary>
        /// The id of the outgoing message.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// The headers of the outgoing message.
        /// </summary>
        Dictionary<string, string> Headers { get; }
    }

    /// <summary>
    /// The abstract base context for everything inside the outgoing pipeline.
    /// </summary>
    abstract class OutgoingContextImpl : BehaviorContextImpl, OutgoingContext
    {
        /// <summary>
        /// Initializes a new <see cref="OutgoingContextImpl"/>.
        /// </summary>
        /// <param name="messageId">The id of the outgoing message.</param>
        /// <param name="headers">The headers of the outgoing message.</param>
        /// <param name="parentContext">The parent context.</param>
        protected OutgoingContextImpl(string messageId, Dictionary<string, string> headers, BehaviorContext parentContext)
            : base(parentContext)
        {
            MessageId = messageId;
            Headers = headers;
        }

        /// <summary>
        /// The id of the outgoing message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// The headers of the outgoing message.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <inheritdoc/>
        public Task SendAsync(object message, SendOptions options)
        {
            return BusOperationsBehaviorContext.SendAsync(this, message, options);
        }

        /// <inheritdoc/>
        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperationsBehaviorContext.SendAsync(this, messageConstructor, options);
        }

        /// <inheritdoc/>
        public Task PublishAsync(object message, PublishOptions options)
        {
            return BusOperationsBehaviorContext.PublishAsync(this, message, options);
        }

        /// <inheritdoc/>
        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperationsBehaviorContext.PublishAsync(this, messageConstructor, publishOptions);
        }

        /// <inheritdoc/>
        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            return BusOperationsBehaviorContext.SubscribeAsync(this, eventType, options);
        }

        /// <inheritdoc/>
        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            return BusOperationsBehaviorContext.UnsubscribeAsync(this, eventType, options);
        }
    }
}