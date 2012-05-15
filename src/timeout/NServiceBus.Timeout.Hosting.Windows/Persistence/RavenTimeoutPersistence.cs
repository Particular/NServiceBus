namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Raven.Abstractions.Commands;
    using Raven.Client;
    using log4net;
    using Raven.Client.Linq;

    public class RavenTimeoutPersistence : IPersistTimeouts
    {
        readonly IDocumentStore store;

        public RavenTimeoutPersistence(IDocumentStore store)
        {
            this.store = store;
        }

        public IEnumerable<TimeoutData> GetAll()
        {
            try 
	        {
                using (var session = OpenSession())
                    return session.Query<TimeoutData>().ToList();
		
	        }
	        catch (Exception)
	        {
                if ((store == null) || (string.IsNullOrWhiteSpace(store.Identifier)) || (string.IsNullOrWhiteSpace(store.Url)))
                {
                    Logger.Error("Exception occurred while trying to access Raven Database. You can check Raven availability at its console at http://localhost:8080/raven/studio.html (unless Raven defaults were changed), or make sure the Raven service is running at services.msc (services programs console).");
                    throw;
                }

                Logger.ErrorFormat(
                    "Exception occurred while trying to access Raven Database: [{0}] at [{1}]. You can check Raven availability at its console at http://localhost:8080/raven/studio.html (unless Raven defaults were changed), or make sure the Raven service is running at services.msc (services programs console).",
                    store.Identifier, store.Url);
		        
                throw;
	        }
        }
        public void Add(TimeoutData timeout)
        {
            using (var session = OpenSession())
            {
                session.Store(timeout);
                session.SaveChanges();
            }
        }

        public void Remove(string timeoutId)
        {
            using (var session = OpenSession())
            {
                session.Advanced.Defer(new DeleteCommandData { Key = timeoutId });
                session.SaveChanges();
            }

        }

        public void ClearTimeoutsFor(Guid sagaId)
        {
            using (var session = OpenSession())
            {
                var items = session.Query<TimeoutData>().Where(x => x.SagaId == sagaId);
                foreach (var item in items)
                    session.Delete(item);

                session.SaveChanges();
            }
        }

        IDocumentSession OpenSession()
        {
            var session = store.OpenSession();

            session.Advanced.AllowNonAuthoritativeInformation = false;

            return session;
        }
        static readonly ILog Logger = LogManager.GetLogger("RavenTimeoutPersistence");
    }
}