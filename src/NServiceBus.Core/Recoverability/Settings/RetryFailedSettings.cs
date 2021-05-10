namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Faults;
    using Settings;

    /// <summary>
    /// Configuration settings for retry faults.
    /// </summary>
    public partial class RetryFailedSettings : ExposeSettings
    {
        internal RetryFailedSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures a header customization action which gets called after all fault headers have been applied.
        /// </summary>
        /// <param name="customization">The customization action.</param>
        public RetryFailedSettings HeaderCustomization(Action<Dictionary<string, string>> customization)
        {
            Guard.AgainstNull(nameof(customization), customization);

            Settings.Set(RecoverabilityComponent.FaultHeaderCustomization, customization);

            return this;
        }

        /// <summary>
        /// Registers a callback when a message fails processing and will be moved to the error queue.
        /// </summary>
        public RetryFailedSettings OnMessageSentToErrorQueue(Func<FailedMessage, CancellationToken, Task> notificationCallback)
        {
            Guard.AgainstNull(nameof(notificationCallback), notificationCallback);

            var subscriptions = Settings.Get<RecoverabilityComponent.Configuration>();
            subscriptions.MessageFaultedNotification.Subscribe((faulted, cancellationToken) =>
            {
                var headerCopy = new Dictionary<string, string>(faulted.Message.Headers);
                var bodyCopy = faulted.Message.Body.Copy();
                return notificationCallback(new FailedMessage(faulted.Message.MessageId, headerCopy, bodyCopy, faulted.Exception, faulted.ErrorQueue), cancellationToken);
            });

            return this;
        }
    }
}