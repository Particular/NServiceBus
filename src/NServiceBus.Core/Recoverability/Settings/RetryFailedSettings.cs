namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Faults;
    using Settings;

    /// <summary>
    /// Configuration settings for retry faults.
    /// </summary>
    public class RetryFailedSettings : ExposeSettings
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
        /// 
        /// </summary>
        public RetryFailedSettings OnMessageSentToErrorQueue(Func<FailedMessage, Task> notificationCallback)
        {
            var subscriptions = Settings.Get<NotificationSubscriptions>();

            subscriptions.Subscribe<MessageFaulted>(faulted =>
            {
                var headerCopy = new Dictionary<string, string>(faulted.Message.Headers);
                var bodyCopy = CopyOfBody(faulted.Message.Body);
                return notificationCallback(new FailedMessage(faulted.Message.MessageId, headerCopy, bodyCopy, faulted.Exception, faulted.ErrorQueue));
            });

            return this;

            byte[] CopyOfBody(byte[] body)
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
}