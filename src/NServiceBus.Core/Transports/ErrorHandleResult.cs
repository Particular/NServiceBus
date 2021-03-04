namespace NServiceBus.Transport
{
    /// <summary>
    /// Provides information about error handling.
    /// </summary>
    public enum ErrorHandleResult
    {
        /// <summary>
        /// The message was discarded.
        /// </summary>
        Discarded,

        /// <summary>
        /// The message was moved to the error queue.
        /// </summary>
        MovedToErrorQueue,

        /// <summary>
        /// The message was queued for a delayed retry.
        /// </summary>
        QueuedForDelayedRetry,

        /// <summary>
        /// Indicates that the infrastructure did not handle the current error. A retry is required.
        /// </summary>
        RetryRequired
    }
}