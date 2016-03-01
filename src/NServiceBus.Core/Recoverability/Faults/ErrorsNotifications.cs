namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline;

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
        public event EventHandler<SecondLevelRetry> MessageHasBeenSentToSecondLevelRetries;

        internal void InvokeMessageHasBeenSentToErrorQueue(ITransportReceiveContext context, Exception exception)
        {
            var failedMessage = new FailedMessage(
                context.MessageId,
                new Dictionary<string, string>(context.Headers),
                CopyOfBody(context.Body), exception);
            MessageSentToErrorQueue?.Invoke(this, failedMessage);
        }

        internal void InvokeMessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, ITransportReceiveContext context, Exception exception)
        {
            var firstLevelRetry = new FirstLevelRetry(
                context.MessageId,
                new Dictionary<string, string>(context.Headers),
                CopyOfBody(context.Body),
                exception,
                firstLevelRetryAttempt);
            MessageHasFailedAFirstLevelRetryAttempt?.Invoke(this, firstLevelRetry);
        }

        internal void InvokeMessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, ITransportReceiveContext context, Exception exception)
        {
            var secondLevelRetry = new SecondLevelRetry(
                new Dictionary<string, string>(context.Headers),
                CopyOfBody(context.Body),
                exception,
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