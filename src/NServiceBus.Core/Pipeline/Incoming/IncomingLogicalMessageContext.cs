namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A context of behavior execution in logical message processing stage.
    /// </summary>
    public interface IncomingLogicalMessageContext : IncomingContext
    {
        /// <summary>
        /// Message being handled.
        /// </summary>
        LogicalMessage Message { get; }

        /// <summary>
        /// Headers for the incoming message.
        /// </summary>
        Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Tells if the message has been handled.
        /// </summary>
        bool MessageHandled { get; set; }
    }
}