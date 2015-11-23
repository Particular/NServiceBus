namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A context of handling a logical message by a handler.
    /// </summary>
    public class InvokeHandlerContext : IncomingContext
    {
        /// <summary>
        /// Initializes the handling stage context. This is the constructor to use for internal usage.
        /// </summary>
        internal InvokeHandlerContext(MessageHandler handler, LogicalMessageProcessingContext parentContext)
            : this(handler, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Headers, parentContext.Message.Metadata, parentContext.Message.Instance, parentContext)
        {
        }

        /// <summary>
        /// Initializes the handling stage context.
        /// </summary>
        public InvokeHandlerContext(MessageHandler handler, string messageId, string replyToAddress, Dictionary<string, string> headers, MessageMetadata messageMetadata, object messageBeingHandled, BehaviorContext parentContext)
            : base(messageId, replyToAddress, headers, parentContext)
        {
            MessageHandler = handler;
            Headers = headers;
            MessageBeingHandled = messageBeingHandled;
            MessageMetadata = messageMetadata;
        }

        /// <summary>
        /// The current <see cref="IHandleMessages{T}" /> being executed.
        /// </summary>
        public MessageHandler MessageHandler { get; }

        /// <summary>
        /// Message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        public object MessageBeingHandled { get; }

        /// <summary>
        /// Returns if further handler invocation has been aborted.
        /// </summary>
        public bool HandlerInvocationAborted { get; private set; }

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        public MessageMetadata MessageMetadata { get; }

        internal void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            HandlerInvocationAborted = true;
        }
    }
}