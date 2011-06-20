namespace NServiceBus.Gateway.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public interface IPersistMessages
    {
        bool InsertMessage(string clientId,DateTime timeReceived, Stream message, IDictionary<string,string> headers);

        bool AckMessage(string clientId, out byte[] message, out  IDictionary<string, string> headers);

        void UpdateHeader(string clientId, string headerKey, string newValue);
    }
}