namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A context of behavior execution in logical message processing stage.
    /// </summary>
    public class IncomingLogicalMessageContext : IncomingContext, IIncomingLogicalMessageContext
    {
        internal IncomingLogicalMessageContext(LogicalMessage logicalMessage, IIncomingPhysicalMessageContext parentContext)
            : this(logicalMessage, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.Message.Headers, parentContext)
        {
        }

        /// <summary>
        /// Creates a new instance of an incoming logical message context.
        /// </summary>
        /// <param name="logicalMessage">The logical message.</param>
        /// <param name="messageId">The message id.</param>
        /// <param name="replyToAddress">The reply to address.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="parentContext">The parent context.</param>
        public IncomingLogicalMessageContext(LogicalMessage logicalMessage, string messageId, string replyToAddress, Dictionary<string, string> headers, IBehaviorContext parentContext)
            : base(messageId, replyToAddress, headers, parentContext)
        {
            Message = logicalMessage;
            Headers = headers;
            Set(logicalMessage);
        }

        /// <summary>
        /// Message being handled.
        /// </summary>
        public LogicalMessage Message { get; }

        /// <summary>
        /// Headers for the incoming message.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Tells if the message has been handled.
        /// </summary>
        public bool MessageHandled { get; set; }
    }
}