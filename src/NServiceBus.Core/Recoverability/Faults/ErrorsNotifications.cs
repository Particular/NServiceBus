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
        /// Notification when a message fails a immediate retry.
        /// </summary>
        public event EventHandler<ImmediateRetry> MessageHasFailedAnImmediateRetryAttempt;

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0",
            ReplacementTypeOrMember = nameof(MessageHasFailedAnImmediateRetryAttempt))]
#pragma warning disable 1591
        public EventHandler MessageHasFailedAFirstLevelRetryAttempt;
#pragma warning restore 1591

        /// <summary>
        /// Notification when a message is sent to Delayed Retries queue.
        /// </summary>
        public event EventHandler<DelayedRetry> MessageHasBeenSentToDelayedRetries;


        [ObsoleteEx(
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0",
            ReplacementTypeOrMember = nameof(MessageHasBeenSentToDelayedRetries)
            )]
#pragma warning disable 1591
        public EventHandler MessageHasBeenSentToSecondLevelRetries;
#pragma warning restore 1591

        internal void InvokeMessageHasBeenSentToErrorQueue(IncomingMessage message, Exception exception, string errorQueue)
        {
            var failedMessage = new FailedMessage(
                message.MessageId,
                new Dictionary<string, string>(message.Headers),
                CopyOfBody(message.Body), exception, errorQueue);
            MessageSentToErrorQueue?.Invoke(this, failedMessage);
        }

        internal void InvokeMessageHasFailedAnImmediateRetryAttempt(int immediateRetryAttempt, IncomingMessage message, Exception exception)
        {
            var retry = new ImmediateRetry(
                message.MessageId,
                new Dictionary<string, string>(message.Headers),
                CopyOfBody(message.Body),
                exception,
                immediateRetryAttempt);
            MessageHasFailedAnImmediateRetryAttempt?.Invoke(this, retry);
        }

        internal void InvokeMessageHasBeenSentToDelayedRetries(int delayedRetryAttempt, IncomingMessage message, Exception exception)
        {
            var retry = new DelayedRetry(
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