namespace NServiceBus.Gateway.Deduplication
{
    using System;
    using NServiceBus.Persistence.Raven;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    [ObsoleteEx]
    public class RavenDBDeduplication : IDeduplicateMessages
    {
        public RavenDBDeduplication(StoreAccessor storeAccessor)
        {
            store = storeAccessor.Store;
        }

        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            using (var session = store.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Advanced.AllowNonAuthoritativeInformation = false;

                session.Store(new GatewayMessage { Id = EscapeClientId(clientId), TimeReceived = timeReceived });

                try
                {
                    session.SaveChanges();
                }
                catch (ConcurrencyException)
                {
                    return false;
                }

                return true;
            }
        }

        static string EscapeClientId(string clientId)
        {
            return clientId.Replace("\\", "_");
        }


        readonly IDocumentStore store;
    }
}
