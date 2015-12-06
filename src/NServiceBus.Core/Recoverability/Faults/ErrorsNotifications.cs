namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transports;

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
        /// Notification when a message is sent to second level retires queue.
        /// </summary>
        public EventHandler<SecondLevelRetry> MessageHasBeenSentToSecondLevelRetries;

        internal void InvokeMessageHasBeenSentToErrorQueue(IncomingMessage message, Exception exception)
        {
            if (MessageSentToErrorQueue != null)
            {
                var failedMessage = new FailedMessage(
                    message.MessageId,
                    new Dictionary<string, string>(message.Headers),
                    CopyOfBody(message.Body), exception);
                MessageSentToErrorQueue?.Invoke(this, failedMessage);
            }
        }

        internal void InvokeMessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, IncomingMessage message, Exception exception)
        {
            if (MessageHasFailedAFirstLevelRetryAttempt != null)
            {
                var firstLevelRetry = new FirstLevelRetry(
                    message.MessageId,
                    new Dictionary<string, string>(message.Headers),
                    CopyOfBody(message.Body),
                    exception,
                    firstLevelRetryAttempt);
                MessageHasFailedAFirstLevelRetryAttempt?.Invoke(this, firstLevelRetry);
            }
        }

        internal void InvokeMessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, IncomingMessage message, Exception exception)
        {
            if (MessageHasBeenSentToSecondLevelRetries != null)
            {
                var secondLevelRetry = new SecondLevelRetry(
                    new Dictionary<string, string>(message.Headers),
                    CopyOfBody(message.Body),
                    exception,
                    secondLevelRetryAttempt);

                MessageHasBeenSentToSecondLevelRetries?.Invoke(this, secondLevelRetry);
            }
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