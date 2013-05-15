namespace NServiceBus.Gateway.Deduplication
{
    using System;

    public interface IDeduplicateMessages
    {
        bool DeduplicateMessage(string clientId, DateTime timeReceived);
    }
}