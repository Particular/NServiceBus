namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Faults;
    using Settings;

    /// <summary>
    /// Configuration settings for Immediate Retries.
    /// </summary>
    public class ImmediateRetriesSettings : ExposeSettings
    {
        internal ImmediateRetriesSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the amount of times a message should be immediately retried after failing
        /// before escalating to Delayed Retries.
        /// </summary>
        /// <param name="numberOfRetries">The number of times to immediately retry a failed message.</param>
        public void NumberOfRetries(int numberOfRetries)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);

            Settings.Set(RecoverabilityComponent.NumberOfImmediateRetries, numberOfRetries);
        }

        /// <summary>
        /// Registers a callback which is invoked when a message fails processing and will be immediately retried.
        /// </summary>
        public ImmediateRetriesSettings OnMessageBeingRetried(Func<ImmediateRetryMessage, Task> notificationCallback)
        {
            Guard.AgainstNull(nameof(notificationCallback), notificationCallback);

            var subscriptions = Settings.Get<RecoverabilityComponent.Configuration>();
            subscriptions.MessageRetryNotification.Subscribe(retry =>
            {
                if (!retry.IsImmediateRetry)
                {
                    return Task.CompletedTask;
                }

                var headerCopy = new Dictionary<string, string>(retry.Message.Headers);
                var bodyCopy = retry.Message.Body.Copy();
                return notificationCallback(new ImmediateRetryMessage(retry.Message.MessageId, headerCopy, bodyCopy, retry.Exception, retry.Attempt));
            });

            return this;
        }
    }
}