namespace NServiceBus.Outbox
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using Settings;

    /// <summary>
    /// Custom settings related to the outbox feature
    /// </summary>
    public class OutboxSettings : ExposeSettings
    {
        internal OutboxSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Specifies how long the outbox should keep message data in storage to be able to deduplicate.
        /// </summary>
        /// <param name="time">The new duration to be used </param>
        [ObsoleteEx(ReplacementTypeOrMember = "InMemoryOutboxSettingsExtensions.TimeToKeepDeduplicationData(TimeSpan time)", TreatAsErrorFromVersion = "6.0", RemoveInVersion = "7.0")]
        // ReSharper disable once UnusedParameter.Global
        public void TimeToKeepDeduplicationData(TimeSpan time)
        {
            throw new NotImplementedException();
        }
    }
}