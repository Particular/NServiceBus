namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Faults;
    using Settings;

    /// <summary>
    /// Configuration settings for Delayed Retries.
    /// </summary>
    public class DelayedRetriesSettings : ExposeSettings
    {
        internal DelayedRetriesSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the number of times a message should be retried with a delay after failing Immediate Retries.
        /// </summary>
        public DelayedRetriesSettings NumberOfRetries(int numberOfRetries)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);

            Settings.Set(RecoverabilityComponent.NumberOfDelayedRetries, numberOfRetries);

            return this;
        }

        /// <summary>
        /// Configures the delay interval increase for each failed Delayed Retries attempt.
        /// </summary>
        public DelayedRetriesSettings TimeIncrease(TimeSpan timeIncrease)
        {
            Guard.AgainstNegative(nameof(timeIncrease), timeIncrease);

            Settings.Set(RecoverabilityComponent.DelayedRetriesTimeIncrease, timeIncrease);

            return this;
        }

        /// <summary>
        /// Registers a callback which is invoked when a message fails processing and will be retried after a delay.
        /// </summary>
        public DelayedRetriesSettings OnMessageBeingRetried(Func<DelayedRetryMessage, Task> notificationCallback)
        {
            Guard.AgainstNull(nameof(notificationCallback), notificationCallback);

            var subscriptions = Settings.Get<RecoverabilityComponent.Configuration>();
            subscriptions.MessageRetryNotification.Subscribe((retry, _) =>
            {
                if (retry.IsImmediateRetry)
                {
                    return Task.CompletedTask;
                }

                var headerCopy = new Dictionary<string, string>(retry.Message.Headers);
                var bodyCopy = retry.Message.Body.Copy();
                return notificationCallback(new DelayedRetryMessage(retry.Message.MessageId, headerCopy, bodyCopy, retry.Exception, retry.Attempt));
            });

            return this;
        }
    }
}