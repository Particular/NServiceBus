namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// 
    /// </summary>
    public interface InvokeHandlerContext :  IMessageHandlerContext, IncomingContext
    {
        /// <summary>
        /// The current <see cref="IHandleMessages{T}" /> being executed.
        /// </summary>
        MessageHandler MessageHandler { get; }

        /// <summary>
        /// Message headers.
        /// </summary>
        Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        object MessageBeingHandled { get; }

        /// <summary>
        /// </summary>
        bool HandlerInvocationAborted { get; }

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        MessageMetadata MessageMetadata { get; }
    }

    /// <summary>
    /// A context of handling a logical message by a handler.
    /// </summary>
    class InvokeHandlerContextImpl : IncomingContextBase, InvokeHandlerContext
    {
        /// <summary>
        /// Initializes the handling stage context. This is the constructor to use for internal usage.
        /// </summary>
        internal InvokeHandlerContextImpl(MessageHandler handler, LogicalMessageProcessingContext parentContext)
            : this(handler, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Headers, parentContext.Message.Metadata, parentContext.Message.Instance, parentContext.PipelineInfo, parentContext)
        {
        }

        /// <summary>
        /// Initializes the handling stage context.
        /// </summary>
        public InvokeHandlerContextImpl(MessageHandler handler, string messageId, string replyToAddress, Dictionary<string, string> headers, MessageMetadata messageMetadata, object messageBeingHandled, PipelineInfo pipelineInfo, BehaviorContext parentContext)
            : base(messageId, replyToAddress, headers, pipelineInfo, parentContext)
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
        /// <code>true</code> if <see cref="DoNotContinueDispatchingCurrentMessageToHandlers" />  has been called.
        /// </summary>
        public bool HandlerInvocationAborted { get; private set; }

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        public MessageMetadata MessageMetadata { get; }

        /// <inheritdoc />
        public virtual Task HandleCurrentMessageLaterAsync()
        {
            return BusOperationsInvokeHandlerContext.HandleCurrentMessageLaterAsync(this);
        }

        /// <inheritdoc />
        public virtual void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            HandlerInvocationAborted = true;
        }

        internal bool handleCurrentMessageLaterWasCalled;
    }
}