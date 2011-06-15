namespace NServiceBus.Gateway.Persistence.Raven
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using global::Raven.Client;
    using global::Raven.Http.Exceptions;
    using Persistence;

    public class RavenDBPersistence : IPersistMessages
    {
        readonly IDocumentStore store;

        public RavenDBPersistence(IDocumentStore store)
        {
            this.store = store;
        }

        public bool InsertMessage(string clientId, DateTime timeReceived, Stream messageStream, IDictionary<string, string> headers)
        {
            var gatewayMessage = new GatewayMessage
                                     {
                                         Id = clientId,
                                         TimeReceived = timeReceived,
                                         Headers = headers,
                                         OriginalMessage = new byte[messageStream.Length]
                                     };

            messageStream.Read(gatewayMessage.OriginalMessage, 0, (int)messageStream.Length);
            using (var session = store.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Store(gatewayMessage);

                //todo
                //documentSession.Advanced.DatabaseCommands.PutAttachment("calls/" + message.CallId, null, mp3, new Raven.Json.Linq.RavenJObject());
                //var attachment = session.Advanced.DatabaseCommands.GetAttachment(id);

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

        public void AckMessage(string clientId, out byte[] message, out IDictionary<string, string> headers)
        {
            using (var session = store.OpenSession())
            {
                var storedMesssage= session.Load<GatewayMessage>(clientId);

                message = storedMesssage.OriginalMessage;
                headers = storedMesssage.Headers;
            }
        }

        public void UpdateHeader(string clientId, string headerKey, string newValue)
        {
            using (var session = store.OpenSession())
            {
                session.Load<GatewayMessage>(clientId).Headers[headerKey] = newValue;

                session.SaveChanges();
            }
        }
    }
}