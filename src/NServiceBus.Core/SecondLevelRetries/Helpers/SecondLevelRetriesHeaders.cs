namespace NServiceBus.SecondLevelRetries.Helpers
{
    static class SecondLevelRetriesHeaders
    {
        public const string RetriesTimestamp = "NServiceBus.Retries.Timestamp";
        public const string RetriesRetryAt = "NServiceBus.Retries.RetryAt";
    }
}