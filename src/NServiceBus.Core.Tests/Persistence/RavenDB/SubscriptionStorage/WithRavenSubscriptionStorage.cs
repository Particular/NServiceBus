namespace NServiceBus.Core.Tests.Persistence.RavenDB.SubscriptionStorage
{
    using NServiceBus.Persistence.Raven;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;
    using Transports.Msmq;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.Raven;

    public class WithRavenSubscriptionStorage
    {
        protected ISubscriptionStorage storage;
        protected IDocumentStore store;

        [SetUp]
        public void SetupContext()
        {
            Address.SetParser<MsmqAddress>();

            store = new EmbeddableDocumentStore { RunInMemory = true};
            store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.QueryYourWrites;
           
            store.Initialize();

            storage = new RavenSubscriptionStorage(new StoreAccessor(store));
            storage.Init();
        }

        [TearDown]
        public void Cleanup()
        {
            store.Dispose();
        }
    }
}