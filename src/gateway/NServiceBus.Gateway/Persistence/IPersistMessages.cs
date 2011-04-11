namespace NServiceBus.Gateway.Persistence
{
    using System;
    using System.Collections.Specialized;

    public interface IPersistMessages
    {
        bool InsertMessage(string clientId,DateTime timeReceived, byte[] message, NameValueCollection headers);

        void AckMessage(string clientId, out byte[] message, out NameValueCollection headers);

        void UpdateHeader(string clientId, string headerKey, string newValue);
    }
}