namespace NServiceBus.InMemory.Outbox
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Outbox;

    /// <summary>
    /// Contains InMemoryOutbox related settings extensions.
    /// </summary>
    public static class InMemoryOutboxSettingsExtensions
    {
        /// <summary>
        /// Specifies how long the outbox should keep message data in storage to be able to deduplicate.
        /// </summary>
        /// <param name="settings">The outbox settings</param>
        /// <param name="time">Defines the timespan which indicates how long the outbox deduplication entries should be kept. 
        /// i.ex if <code>TimeSpan.FromDays(1)</code> is used the deduplication entries are kept for no longer than one day.
        /// It is not possible to use a negative or zero TimeSpan value.</param>
        public static OutboxSettings TimeToKeepDeduplicationData(this OutboxSettings settings, TimeSpan time)
        {
            Guard.AgainstNegativeAndZero(time, "time");
            settings.GetSettings().Set(Features.InMemoryOutboxPersistence.TimeToKeepDeduplicationEntries, time);
            return settings;
        }
    }
}