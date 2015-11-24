namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Unicast;
    using PublishOptions = NServiceBus.PublishOptions;
    using ReplyOptions = NServiceBus.ReplyOptions;
    using SendOptions = NServiceBus.SendOptions;

    /// <summary>
    /// The abstract base context for everything after the transport receive phase.
    /// </summary>
    public abstract class IncomingContext : BehaviorContext, IMessageProcessingContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IncomingContext" />.
        /// </summary>
        protected IncomingContext(string messageId, string replyToAddress, IReadOnlyDictionary<string, string> headers, BehaviorContext parentContext)
            : base(parentContext)
        {

            MessageId = messageId;
            ReplyToAddress = replyToAddress;
            MessageHeaders = headers;
        }

        /// <inheritdoc />
        public ContextBag Extensions => this;

        /// <inheritdoc />
        public string MessageId { get; }

        /// <inheritdoc />
        public string ReplyToAddress { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }

        /// <inheritdoc />
        public Task Send(object message, SendOptions options)
        {
            return BusOperationsBehaviorContext.Send(this, message, options);
        }

        /// <inheritdoc />
        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperationsBehaviorContext.Send(this, messageConstructor, options);
        }

        /// <inheritdoc />
        public Task Publish(object message, PublishOptions options)
        {
            return BusOperationsBehaviorContext.Publish(this, message, options);
        }

        /// <inheritdoc />
        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperationsBehaviorContext.Publish(this, messageConstructor, publishOptions);
        }

        /// <inheritdoc />
        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperationsBehaviorContext.Subscribe(this, eventType, options);
        }

        /// <inheritdoc />
        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperationsBehaviorContext.Unsubscribe(this, eventType, options);
        }

        /// <inheritdoc />
        public Task Reply(object message, ReplyOptions options)
        {
            return BusOperationsBehaviorContext.Reply(this, message, options);
        }

        /// <inheritdoc />
        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return BusOperationsBehaviorContext.Reply(this, messageConstructor, options);
        }

        /// <inheritdoc />
        public Task ForwardCurrentMessageTo(string destination)
        {
            return BusOperationsIncomingContext.ForwardCurrentMessageTo(this, destination);
        }
    }
}