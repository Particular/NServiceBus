namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    /// <summary>
    /// A context of behavior execution in logical message processing stage.
    /// </summary>
    public interface IIncomingLogicalMessageContext : IIncomingContext
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