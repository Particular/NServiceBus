namespace NServiceBus.Gateway.Persistence.Raven
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using global::Raven.Abstractions.Exceptions;
    using global::Raven.Client;
    using NServiceBus.Persistence.Raven;

    public class RavenDbPersistence : IPersistMessages
    {
        public RavenDbPersistence(StoreAccessor storeAccessor)
        {
            store = storeAccessor.Store;
        }

        public bool InsertMessage(string clientId, DateTime timeReceived, Stream messageStream, IDictionary<string, string> headers)
        {
            var gatewayMessage = new GatewayMessage
                                     {
                                         Id = EscapeClientId(clientId),
                                         TimeReceived = timeReceived,
                                         Headers = headers,
                                         OriginalMessage = new byte[messageStream.Length],
                                         Acknowledged = false
                                     };

            messageStream.Read(gatewayMessage.OriginalMessage, 0, (int)messageStream.Length);
            using (var session = OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Store(gatewayMessage);

                try
                {
                    session.SaveChanges();
                }
                catch (ConcurrencyException)
                {
                    return false;
                }

            }
            return true;
        }

        public bool AckMessage(string clientId, out byte[] message, out IDictionary<string, string> headers)
        {
            message = null;
            headers = null;

            using (var session = OpenSession())
            {
                var storedMessage = session.Load<GatewayMessage>(EscapeClientId(clientId));

                if (storedMessage == null)
                    throw new InvalidOperationException("No message with id: " + clientId+ "found");

                if (storedMessage.Acknowledged)
                    return false;

                message = storedMessage.OriginalMessage;
                headers = storedMessage.Headers;

                storedMessage.Acknowledged = true;
               
                session.SaveChanges();

                return true;
            }
        }

        public void UpdateHeader(string clientId, string headerKey, string newValue)
        {
            using (var session = OpenSession())
            {
                session.Load<GatewayMessage>(EscapeClientId(clientId)).Headers[headerKey] = newValue;

                session.SaveChanges();
            }
        }

        public static string EscapeClientId(string clientId)
        {
            return clientId.Replace("\\", "_");
        }

        IDocumentSession OpenSession()
        {
            var session = store.OpenSession();

            session.Advanced.AllowNonAuthoritativeInformation = false;

            return session;
        }

        private readonly IDocumentStore store;
    }
}