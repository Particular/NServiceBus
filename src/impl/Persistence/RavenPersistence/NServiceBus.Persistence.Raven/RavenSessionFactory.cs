using System;
using Raven.Client;

namespace NServiceBus.Persistence.Raven
{
    public class RavenSessionFactory : IDisposable
    {
        IDocumentSession session;

        public IDocumentStore Store { get; private set; }

        public IDocumentSession Session
        {
            get { return session ?? (session = OpenSession()); }
        }
        
        public RavenSessionFactory(IDocumentStore store)
        {
            this.Store = store;
        }

        public void Dispose()
        {
            if (session != null)
                session.Dispose();
        }

        IDocumentSession OpenSession()
        {
            var s = Store.OpenSession();
            s.Advanced.AllowNonAuthoritiveInformation = false;
            s.Advanced.UseOptimisticConcurrency = true;

            return s;
        }
    }
}