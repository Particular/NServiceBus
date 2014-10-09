namespace NServiceBus
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public interface IErrorSubscriber
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        void MessageHasBeenSentToErrorQueue(TransportMessage message, Exception exception);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstLevelRetryAttempt"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        void MessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, TransportMessage message, Exception exception);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secondLevelRetryAttempt"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        void MessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, TransportMessage message, Exception exception);
    }
}