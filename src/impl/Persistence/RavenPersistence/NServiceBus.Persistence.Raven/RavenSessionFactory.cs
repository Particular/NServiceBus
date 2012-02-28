using System;
using Raven.Client;

namespace NServiceBus.Persistence.Raven
{
    public class RavenSessionFactory : IDisposable
    {
        IDocumentSession session;

        public IDocumentStore Store { get; private set; }

        public IBus Bus { get; set; }

        public IDocumentSession Session
        {
            get { return session ?? (session = OpenSession()); }
        }
        
        public RavenSessionFactory(IDocumentStore store)
        {
            Store = store;
        }

        public void Dispose()
        {
            if (session != null)
                session.Dispose();
        }

        IDocumentSession OpenSession()
        {
            var databaseName = GetDatabaseName(Bus.CurrentMessageContext);

            IDocumentSession documentSession;

            if (string.IsNullOrEmpty(databaseName))
                documentSession = Store.OpenSession();
            else
                documentSession = Store.OpenSession(databaseName);

            documentSession.Advanced.AllowNonAuthoritativeInformation = false;
            documentSession.Advanced.UseOptimisticConcurrency = true;

            return documentSession;
        }

        public void SaveChanges()
        {
            if (session != null)
                session.SaveChanges();
        }

        public static Func<IMessageContext,string> GetDatabaseName = (context) => "";
    }
}