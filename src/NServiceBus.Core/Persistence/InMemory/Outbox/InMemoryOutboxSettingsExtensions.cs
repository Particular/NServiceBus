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
        /// <param name="cfg">The outbox settings</param>
        /// <param name="time">The new duration to be used </param>
        public static OutboxSettings TimeToKeepDeduplicationData(this OutboxSettings cfg, TimeSpan time)
        {
            Guard.AgainstNegativeAndZero(time, "time");
            cfg.GetSettings().Set(Features.InMemoryOutboxPersistence.TimeToKeepDeduplicationEntries, time);
            return cfg;
        }
    }
}