namespace NServiceBus.Management.Retries
{
    public static class SecondLevelRetriesHeaders
    {
        public const string OriginalReplyToAddress = "NServiceBus.Retries.OriginalReplyToAddress";
        public const string RetriesTimestamp = "NServiceBus.Retries.Timestamp";
    }
}