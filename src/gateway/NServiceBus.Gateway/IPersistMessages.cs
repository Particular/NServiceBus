namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Specialized;

    public interface IPersistMessages
    {
        bool InsertMessage(DateTime dateTime, string clientId, byte[] md5, byte[] message, NameValueCollection headers);

        void AckMessage(string clientId, byte[] md5, out byte[] message, out NameValueCollection headers);
    }
}