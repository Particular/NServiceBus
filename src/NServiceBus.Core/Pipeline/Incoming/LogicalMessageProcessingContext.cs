namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A context of behavior execution in logical message processing stage.
    /// </summary>
    public class LogicalMessageProcessingContext : IncomingContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LogicalMessageProcessingContext" />. This is the constructor to use for internal usage.
        /// </summary>
        /// <param name="logicalMessage">The logical message.</param>
        /// <param name="parentContext">The wrapped context.</param>
        internal LogicalMessageProcessingContext(LogicalMessage logicalMessage, PhysicalMessageProcessingContext parentContext)
            : this(logicalMessage, parentContext.Message.Headers, parentContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LogicalMessageProcessingContext" />.
        /// </summary>
        /// <param name="logicalMessage">The logical message.</param>
        /// <param name="headers">The messages headers.</param>
        /// <param name="parentContext">The wrapped context.</param>
        public LogicalMessageProcessingContext(LogicalMessage logicalMessage, Dictionary<string, string> headers, BehaviorContext parentContext)
            : base(parentContext)
        {
            Message = logicalMessage;
            Headers = headers;
        }

        /// <summary>
        /// Message being handled.
        /// </summary>
        public LogicalMessage Message { get; private set; }

        /// <summary>
        /// Headers for the incoming message.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Tells if the message has been handled.
        /// </summary>
        public bool MessageHandled { get; set; }
    }
}