namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    /// Errors notifications.
    /// </summary>
    public class ErrorsNotifications
    {
        /// <summary>
        /// Notification when a message is moved to the error queue.
        /// </summary>
        public event EventHandler<FailedMessage> MessageSentToErrorQueue;

        /// <summary>
        /// Notification when a message fails a first level retry.
        /// </summary>
        public event EventHandler<FirstLevelRetry> MessageHasFailedAFirstLevelRetryAttempt;

        /// <summary>
        /// Notification when a message is sent to second level retries queue.
        /// </summary>
        public event EventHandler<SecondLevelRetry> MessageHasBeenSentToSecondLevelRetries;

        internal void InvokeMessageHasBeenSentToErrorQueue(IncomingMessage message, ExceptionInfo exceptionInfo)
        {
            var failedMessage = new FailedMessage(
                message.MessageId,
                new Dictionary<string, string>(message.Headers),
                CopyOfBody(message.Body), exceptionInfo);
            MessageSentToErrorQueue?.Invoke(this, failedMessage);
        }

        internal void InvokeMessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, IncomingMessage message, ExceptionInfo exceptionInfo)
        {
            var firstLevelRetry = new FirstLevelRetry(
                message.MessageId,
                new Dictionary<string, string>(message.Headers),
                CopyOfBody(message.Body),
                exceptionInfo,
                firstLevelRetryAttempt);
            MessageHasFailedAFirstLevelRetryAttempt?.Invoke(this, firstLevelRetry);
        }

        internal void InvokeMessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, IncomingMessage message, ExceptionInfo exceptionInfo)
        {
            var secondLevelRetry = new SecondLevelRetry(
                new Dictionary<string, string>(message.Headers),
                CopyOfBody(message.Body),
                exceptionInfo,
                secondLevelRetryAttempt);

            MessageHasBeenSentToSecondLevelRetries?.Invoke(this, secondLevelRetry);
        }

        static byte[] CopyOfBody(byte[] body)
        {
            if (body == null)
            {
                return null;
            }

            var copyBody = new byte[body.Length];

            Buffer.BlockCopy(body, 0, copyBody, 0, body.Length);

            return copyBody;
        }
    }
}