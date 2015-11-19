namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Transport;
    using PublishOptions = NServiceBus.PublishOptions;
    using ReplyOptions = NServiceBus.ReplyOptions;
    using SendOptions = NServiceBus.SendOptions;

    /// <summary>
    /// 
    /// </summary>
    public interface IncomingContext : BehaviorContext, IMessageProcessingContext
    {
        /// <summary>
        /// Contains information about the current pipeline.
        /// </summary>
        PipelineInfo PipelineInfo { get; }
    }

    /// <summary>
    /// The abstract base context for everything after the transport receive phase.
    /// </summary>
    public abstract class IncomingContextBase : BehaviorContextImpl, IncomingContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IncomingContextBase" />.
        /// </summary>
        protected IncomingContextBase(string messageId, string replyToAddress, IReadOnlyDictionary<string, string> headers, PipelineInfo pipelineInfo, BehaviorContext parentContext)
            : base(parentContext)
        {

            MessageId = messageId;
            ReplyToAddress = replyToAddress;
            MessageHeaders = headers;
            PipelineInfo = pipelineInfo;
        }

        /// <summary>
        /// Contains information about the current pipeline.
        /// </summary>
        public PipelineInfo PipelineInfo { get; }

        /// <inheritdoc />
        public string MessageId { get; }

        /// <inheritdoc />
        public string ReplyToAddress { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }

        /// <inheritdoc />
        public Task SendAsync(object message, SendOptions options)
        {
            return BusOperationsBehaviorContext.SendAsync(this, message, options);
        }

        /// <inheritdoc />
        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperationsBehaviorContext.SendAsync(this, messageConstructor, options);
        }

        /// <inheritdoc />
        public Task PublishAsync(object message, PublishOptions options)
        {
            return BusOperationsBehaviorContext.PublishAsync(this, message, options);
        }

        /// <inheritdoc />
        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperationsBehaviorContext.PublishAsync(this, messageConstructor, publishOptions);
        }

        /// <inheritdoc />
        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            return BusOperationsBehaviorContext.SubscribeAsync(this, eventType, options);
        }

        /// <inheritdoc />
        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            return BusOperationsBehaviorContext.UnsubscribeAsync(this, eventType, options);
        }

        /// <inheritdoc />
        public Task ReplyAsync(object message, ReplyOptions options)
        {
            return BusOperationsBehaviorContext.ReplyAsync(this, message, options);
        }

        /// <inheritdoc />
        public Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            return BusOperationsBehaviorContext.ReplyAsync(this, messageConstructor, options);
        }

        /// <inheritdoc />
        public Task ForwardCurrentMessageToAsync(string destination)
        {
            return BusOperationsIncomingContext.ForwardCurrentMessageToAsync(this, destination);
        }
    }
}