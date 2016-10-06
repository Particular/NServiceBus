namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    /// Errors notifications.
    /// </summary>
    public partial class ErrorsNotifications
    {
        /// <summary>
        /// Notification when a message is moved to the error queue.
        /// </summary>
        public event EventHandler<FailedMessage> MessageSentToErrorQueue;

        /// <summary>
        /// Notification when a message fails a immediate retry.
        /// </summary>
        public event EventHandler<ImmediateRetryMessage> MessageHasFailedAnImmediateRetryAttempt;

        /// <summary>
        /// Notification when a message is sent to Delayed Retries queue.
        /// </summary>
        public event EventHandler<DelayedRetryMessage> MessageHasBeenSentToDelayedRetries;

        internal void InvokeMessageHasBeenSentToErrorQueue(IncomingMessage message, Exception exception, string errorQueue)
        {
            if (MessageSentToErrorQueue == null)
            {
                return;
            }

            var failedMessage = new FailedMessage(
                message.MessageId,
                new Dictionary<string, string>(message.Headers),
                CopyOfBody(message.Body), exception, errorQueue);
            MessageSentToErrorQueue?.Invoke(this, failedMessage);
        }

        internal void InvokeMessageHasFailedAnImmediateRetryAttempt(int immediateRetryAttempt, IncomingMessage message, Exception exception)
        {
            if (MessageHasFailedAnImmediateRetryAttempt == null)
            {
                return;
            }

            var retry = new ImmediateRetryMessage(
                message.MessageId,
                new Dictionary<string, string>(message.Headers),
                CopyOfBody(message.Body),
                exception,
                immediateRetryAttempt);
            MessageHasFailedAnImmediateRetryAttempt?.Invoke(this, retry);
        }

        internal void InvokeMessageHasBeenSentToDelayedRetries(int delayedRetryAttempt, IncomingMessage message, Exception exception)
        {
            if (MessageHasBeenSentToDelayedRetries == null)
            {
                return;
            }

            var retry = new DelayedRetryMessage(
                new Dictionary<string, string>(message.Headers),
                CopyOfBody(message.Body),
                exception,
                delayedRetryAttempt);

            MessageHasBeenSentToDelayedRetries?.Invoke(this, retry);
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