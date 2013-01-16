namespace NServiceBus.Core.Tests.Persistence.RavenDB.SubscriptionStorage
{
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.Raven;

    public class WithRavenSubscriptionStorage
    {
        protected ISubscriptionStorage storage;
        protected IDocumentStore store;

        [SetUp]
        public void SetupContext()
        {
            store = new EmbeddableDocumentStore { RunInMemory = true};
            store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.QueryYourWrites;
           
            store.Initialize();

            storage = new RavenSubscriptionStorage { Store = store};
            storage.Init();
        }

        [TearDown]
        public void Cleanup()
        {
            store.Dispose();
        }
    }
}