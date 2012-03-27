using System;
using Raven.Client;

namespace NServiceBus.Persistence.Raven
{
    public class RavenSessionFactory : IDisposable
    {
        [ThreadStatic]
        static IDocumentSession session;

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
            if (session == null)
                return;

            session.Dispose();
            session = null;
        }

        IDocumentSession OpenSession()
        {
            IMessageContext context = null;

            if (Bus != null)
                context = Bus.CurrentMessageContext;

            var databaseName = GetDatabaseName(context);

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

        public static Func<IMessageContext, string> GetDatabaseName = (context) => "";
    }
}