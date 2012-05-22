namespace NServiceBus.Timeout.Tests
{
    using Core;
    using Hosting.Windows.Persistence;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Embedded;

    public class WithRavenTimeoutPersister
    {
        protected IPersistTimeouts persister;
        protected IDocumentStore store;

        [TestFixtureSetUp]
        public void SetupContext()
        {
            store = new EmbeddableDocumentStore { RunInMemory = true };
            //store = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "MyServer" };

            store.Initialize();

            persister = new RavenTimeoutPersistence(store);
        }

    }
}