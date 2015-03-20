namespace NServiceBus.Outbox
{
    using System;
    using Settings;

    /// <summary>
    /// Custom settings related to the outbox feature
    /// </summary>
    public class OutboxSettings
    {
        readonly SettingsHolder settings;

        internal OutboxSettings(SettingsHolder settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Specifies how long the outbox should keep message data in storage to be able to deduplicate.
        /// </summary>
        /// <param name="time">The new duration to be used</param>
        public void TimeToKeepDeduplicationData(TimeSpan time)
        {
            Guard.AgainstNegativeAndZero(time, "time");
            settings.Set(Features.Outbox.TimeToKeepDeduplicationEntries,time);
        }
    }
}