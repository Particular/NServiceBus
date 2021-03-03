namespace NServiceBus.Transport
{
    /// <summary>
    /// Provides information about message processing.
    /// </summary>
    public enum ReceiveResult
    {
        /// <summary>
        /// Indicates that the infrastructure succeeded in processing the messsage.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Indicates that the infrastructure discarded the current message.
        /// </summary>
        Discarded,

        /// <summary>
        /// Indicates that the infrastructure moved the current message to the error queue.
        /// </summary>
        MovedToErrorQueue,

        /// <summary>
        /// Indicates that the infrastructure queued the current message for a delayed retry.
        /// </summary>
        QueuedForDelayedRetry,

        /// <summary>
        /// Indicates that the infrastructure was did not handle the current error. A retry is required.
        /// </summary>
        RetryRequired,

        /// <summary>
        /// Indicates that the message expired and was not processed.
        /// </summary>
        Expired
    }
}