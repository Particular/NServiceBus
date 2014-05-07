namespace NServiceBus.Persistence.Raven
{
    using System;
    using global::Raven.Client;

    [ObsoleteEx]
    public class RavenSessionFactory
    {
        [ThreadStatic]
        static IDocumentSession session;

        public IDocumentStore Store { get; private set; }

        public IBus Bus { get; set; }

        public IDocumentSession Session
        {
            get { return session ?? (session = OpenSession()); }
        }

        public RavenSessionFactory(StoreAccessor storeAccessor)
        {
            Store = storeAccessor.Store;
        }

        public void ReleaseSession()
        {
            if (session == null)
            {
                return;
            }

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
            if (session == null)
                return;
            try
            {
                session.SaveChanges();
            }
            catch (global::Raven.Abstractions.Exceptions.ConcurrencyException ex)
            {                
                throw new ConcurrencyException("A saga with the same Unique property already existed in the storage. See the inner exception for further details", ex);
            }
        }

        public static Func<IMessageContext, string> GetDatabaseName = context => String.Empty;
    }
}