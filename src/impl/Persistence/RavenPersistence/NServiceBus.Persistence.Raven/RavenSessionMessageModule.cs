namespace NServiceBus.Persistence.Raven
{
    using System.Threading;
    using global::Raven.Client;

    //todo
    public class RavenSessionMessageModule //: IMessageModule
    {
        readonly ThreadLocal<IDocumentSession> currentSession = new ThreadLocal<IDocumentSession>();

        readonly IDocumentStore store;

        public RavenSessionMessageModule(IDocumentStore store)
        {
            this.store = store;
        }

        public IDocumentSession CurrentSession
        {
            get { return currentSession.Value; }
        }

        public void HandleBeginMessage()
        {
            currentSession.Value = store.OpenSession();
        }

        public void HandleEndMessage()
        {
            using (var old = currentSession.Value)
            {
                currentSession.Value = null;
                old.SaveChanges();
            }
        }

        public void HandleError()
        {
            using (var old = currentSession.Value)
                currentSession.Value = null;
        }
    }
}