namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using Unicast.Messages;

    /// <summary>
    /// A context of behavior execution in logical message processing stage.
    /// </summary>
    public class LogicalMessageProcessingContext : IncomingContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LogicalMessageProcessingContext"/>.
        /// </summary>
        /// <param name="logicalMessage">The logical message.</param>
        /// <param name="headers">The headers for the incoming message.</param>
        /// <param name="parentContext">The wrapped context.</param>
        public LogicalMessageProcessingContext(LogicalMessage logicalMessage, Dictionary<string, string> headers, IncomingContext parentContext)
            : base(parentContext)
        {
            Message = logicalMessage;
            Headers = headers;
         }

        /// <summary>
        /// Message beeing handled.
        /// </summary>
        public LogicalMessage Message { get; private set; }

        /// <summary>
        ///    Headers for the incoming message.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }
        
        /// <summary>
        /// Tells if the message has been handled.
        /// </summary>
        public bool MessageHandled { get; set; }
    }
}