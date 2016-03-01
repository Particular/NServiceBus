namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public interface ITransportReceiveContext : IBehaviorContext
    {
        /// <summary>
        /// The message id of the message being processed.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// The headers of the incoming message.
        /// </summary>
        Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The body of the incoming message.
        /// </summary>
        byte[] Body { get; }

        /// <summary>
        /// Reverts to the original body if needed.
        /// </summary>
        void RevertToOriginalBodyIfNeeded();

        /// <summary>
        /// Allows the pipeline to flag that it has been aborted and the receive operation should be rolled back.
        /// </summary>
        void AbortReceiveOperation();
    }
}