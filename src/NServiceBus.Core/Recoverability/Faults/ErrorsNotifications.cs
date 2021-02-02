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
        [ObsoleteEx(
            Message = "The .NET event based error notifications will be deprecated in favor of Task-based callbacks. Use endpointConfiguration.Recoverability().Failed(settings => settings.OnMessageSentToErrorQueue(callback)) instead.",
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0")]
        public event EventHandler<FailedMessage> MessageSentToErrorQueue;

        /// <summary>
        /// Notification when a message fails a immediate retry.
        /// </summary>
        [ObsoleteEx(
            Message = "The .NET event based error notifications will be deprecated in favor of Task-based callbacks. Use endpointConfiguration.Recoverability().Immediate(settings => settings.OnMessageBeingRetried(callback)) instead.",
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0")]
        public event EventHandler<ImmediateRetryMessage> MessageHasFailedAnImmediateRetryAttempt;

        /// <summary>
        /// Notification when a message is sent to Delayed Retries queue.
        /// </summary>
        [ObsoleteEx(
            Message = "The .NET event based error notifications will be deprecated in favor of Task-based callbacks. Use endpointConfiguration.Recoverability().Delayed(settings => settings.OnMessageBeingRetried(callback)) instead.",
            RemoveInVersion = "9.0",
            TreatAsErrorFromVersion = "8.0")]
        public event EventHandler<DelayedRetryMessage> MessageHasBeenSentToDelayedRetries;

        internal void InvokeMessageHasBeenSentToErrorQueue(IncomingMessage message, Exception exception, string errorQueue)
        {
            MessageSentToErrorQueue?.Invoke(this, new FailedMessage(
                message.MessageId,
                new Dictionary<string, string>(message.Headers),
                message.Body.Copy(),
                exception,
                errorQueue));
        }

        internal void InvokeMessageHasFailedAnImmediateRetryAttempt(int immediateRetryAttempt, IncomingMessage message, Exception exception)
        {
            MessageHasFailedAnImmediateRetryAttempt?.Invoke(this, new ImmediateRetryMessage(
                message.MessageId,
                new Dictionary<string, string>(message.Headers),
                message.Body.Copy(),
                exception,
                immediateRetryAttempt));
        }

        internal void InvokeMessageHasBeenSentToDelayedRetries(int delayedRetryAttempt, IncomingMessage message, Exception exception)
        {
            MessageHasBeenSentToDelayedRetries?.Invoke(this, new DelayedRetryMessage(
                message.MessageId,
                new Dictionary<string, string>(message.Headers),
                message.Body.Copy(),
                exception,
                delayedRetryAttempt));
        }
    }
}