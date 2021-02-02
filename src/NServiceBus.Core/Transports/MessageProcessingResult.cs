namespace NServiceBus.Transport
{
    /// <summary>
    /// Controls how the transport should finalize the processing of the message.
    /// </summary>
    public class MessageProcessingResult
    {
        /// <summary>
        /// Initializes the result.
        /// </summary>
        /// <param name="abortReceiveOperation">Indicates that the message receive operation should be aborted and the message rolled back to the inputqueue.</param>
        public MessageProcessingResult(bool abortReceiveOperation)
        {
            AbortReceiveOperation = abortReceiveOperation;
        }

        /// <summary>
        /// Indicates that the message receive operation should be aborted and the message rolled back to the inputqueue.
        /// </summary>
        public bool AbortReceiveOperation { get; }
    }
}