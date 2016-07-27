namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Configuration.AdvanceExtensibility;
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
        /// Configures the error queue address to which failed messages will be sent to.
        /// </summary>
        /// <param name="errorQueue">The name of the error queue to use.</param>
        public RetryFailedSettings SendTo(string errorQueue)
        {
            Guard.AgainstNullAndEmpty(nameof(errorQueue), errorQueue);

            Settings.Set("errorQueue", errorQueue);

            return this;
        }

        /// <summary>
        /// Configures a header customization action which gets called after all fault headers have been applied.
        /// </summary>
        /// <param name="customization">The customization action.</param>
        public RetryFailedSettings HeaderCustomization(Action<Dictionary<string, string>> customization)
        {
            Guard.AgainstNull(nameof(customization), customization);

            Settings.Set(Recoverability.FaultHeaderCustomization, customization);

            return this;
        }
    }
}