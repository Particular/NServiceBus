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

        /// <summary>
        /// Updates the message instance contained in <see cref="LogicalMessage"/>.
        /// </summary>
        /// <param name="newInstance">The new instance.</param>
        void UpdateMessageInstance(object newInstance);
    }
}