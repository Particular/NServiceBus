namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    /// <summary>
    /// A context of behavior execution in physical message processing stage.
    /// </summary>
    public interface IIncomingPhysicalMessageContext : IIncomingContext
    {
        /// <summary>
        /// The body of the incoming message.
        /// </summary>
        byte[] Body { get; }

        /// <summary>
        /// The headers of the incoming message.
        /// </summary>
        Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Reverts to the original body if needed.
        /// </summary>
        void RevertToOriginalBodyIfNeeded();

        /// <summary>
        /// Updates the message with the given body.
        /// </summary>
        void UpdateMessage(byte[] body);
    }
}