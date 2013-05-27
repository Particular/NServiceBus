namespace NServiceBus.Gateway.Deduplication
{
    using System;
    using NServiceBus.Persistence.Raven;
    using Raven.Client;
    using ConcurrencyException = Raven.Abstractions.Exceptions.ConcurrencyException;

    public class RavenDBDeduplication : IDeduplicateMessages
    {
        public RavenDBDeduplication(StoreAccessor storeAccessor)
        {
            store = storeAccessor.Store;
        }

        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            using (var session = OpenSession())
            {
                var storedMessage = session.Load<GatewayMessage>(EscapeClientId(clientId));
                if (storedMessage != null)
                    return false;

                session.Advanced.UseOptimisticConcurrency = true;
                session.Store(new GatewayMessage { Id = EscapeClientId(clientId), TimeReceived = timeReceived });

                try
                {
                    session.SaveChanges();
                }
                catch (ConcurrencyException)
                {
                    return false;
                }
            }
            return false;
        }

        private static string EscapeClientId(string clientId)
        {
            return clientId.Replace("\\", "_");
        }

        private IDocumentSession OpenSession()
        {
            var session = store.OpenSession();
            session.Advanced.AllowNonAuthoritativeInformation = false;
            return session;
        }

        private readonly IDocumentStore store;
    }
}
