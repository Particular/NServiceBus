namespace NServiceBus.Transport
{
    /// <summary>
    /// Controls how the transport should finalize the processing of the message.
    /// /// </summary>
    public class MessageProcessingResult
    {
        /// <summary>
        /// Initializes the result.
        /// </summary>
        /// <param name="forceMessageRollback">Indicates that the message receive operation should be aborted and the message rolled back to the inputqueue.</param>
        public MessageProcessingResult(bool forceMessageRollback)
        {
            ForceMessageRollback = forceMessageRollback;
        }

        /// <summary>
        /// Indicates that the message receive operation should be aborted and the message rolled back to the inputqueue.
        /// </summary>
        public bool ForceMessageRollback { get; }
    }
}