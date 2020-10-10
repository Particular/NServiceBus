namespace NServiceBus.InMemory.Outbox
{
    using System;
    using NServiceBus.Outbox;

    /// <summary>
    /// Contains InMemoryOutbox-related settings extensions.
    /// </summary>
    [ObsoleteEx(Message = "!!!", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
    public static class InMemoryOutboxSettingsExtensions
    {
        /// <summary>
        /// Specifies how long the outbox should keep message data in storage to be able to deduplicate.
        /// </summary>
        /// <param name="settings">The outbox settings.</param>
        /// <param name="time">
        /// Defines the <see cref="TimeSpan"/> which indicates how long the outbox deduplication entries should be kept.
        /// For example, if <code>TimeSpan.FromDays(1)</code> is used, the deduplication entries are kept for no longer than one day.
        /// It is not possible to use a negative or zero TimeSpan value.
        /// </param>
        [ObsoleteEx(Message = "!!!", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
        public static OutboxSettings TimeToKeepDeduplicationData(this OutboxSettings settings, TimeSpan time)
        {
            throw new NotSupportedException();
        }
    }
}