namespace NServiceBus.Timeout.Tests
{
    using Core;
    using Hosting.Windows.Persistence;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;
    using Raven.Client.Extensions;

    public class WithRavenTimeoutPersister
    {
        protected IPersistTimeouts persister;
        protected IDocumentStore store;

        [SetUp]
        public void SetupContext()
        {
            Configure.GetEndpointNameAction = () => "MyEndpoint";

            store = new EmbeddableDocumentStore { RunInMemory = true };
            //store = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "Test" };
            store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.QueryYourWrites; //This turns on WaitForNonStaleResults() on queries globally
            store.Conventions.MaxNumberOfRequestsPerSession = 10;
            store.Initialize();
            persister = new RavenTimeoutPersistence(store);
        }

        [TearDown]
        public void DisposeStore()
        {
            // This is required otherwise we get:
            // StackTrace of un-disposed document store recorded. Please make sure to dispose any document store in the tests in order to avoid race conditions in tests.
            store.Dispose();
        }
    }
}