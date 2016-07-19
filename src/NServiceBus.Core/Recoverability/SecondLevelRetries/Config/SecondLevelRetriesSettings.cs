namespace NServiceBus
{
    using System;

    /// <summary>
    /// Configuration settings for second level retries.
    /// </summary>
    public class SecondLevelRetriesSettings
    {
        /// <summary>
        /// Registers a custom retry policy.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "configuration.Recoverability().CustomPolicy(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> @custom)")]
        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            throw new NotImplementedException();
        }
    }
}