namespace NServiceBus.Persistence.Raven
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using NServiceBus.DataBus;
    using NServiceBus.Logging;

    using global::Raven.Client;

    public class RavenSessionFactory : IDisposable
    {
        bool disposed;

        [ThreadStatic]
        static IDocumentSession session;

        public IDocumentStore Store { get; private set; }
        public IBus Bus { get; set; }
        public ConcurrentDictionary<IDocumentSession, IDocumentSession> openSessions = new ConcurrentDictionary<IDocumentSession, IDocumentSession>();
        private readonly ILog logger = LogManager.GetLogger(typeof(RavenSessionFactory));

        public IDocumentSession Session
        {
            get { return session ?? (session = OpenSession()); }
        }

        public RavenSessionFactory(IDocumentStore store)
        {
            Store = store;
        }

        public void ReleaseSession()
        {
            if (session == null)
            {
                return;
            }

            session.Dispose();

            IDocumentSession tempSession;
            this.openSessions.TryRemove(session, out tempSession);

            session = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (KeyValuePair<IDocumentSession, IDocumentSession> documentSession in openSessions)
                {
                    logger.Warn("Unexpected open RavenDB session found during shutdown - Disposing.");
                    documentSession.Key.Dispose();
                }

                this.Store.Dispose();
            }

            disposed = true;
        }

        ~RavenSessionFactory()
        {
            Dispose(false);
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

            openSessions.TryAdd(documentSession, documentSession);
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