namespace NServiceBus.Core.Tests.Persistence.RavenDB.SubscriptionStorage
{
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Embedded;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.Raven;

    public class WithRavenSubscriptionStorage
    {
        protected ISubscriptionStorage storage;
        protected IDocumentStore store;
        
        [TestFixtureSetUp]
        public void SetupContext()
        {
            store = new EmbeddableDocumentStore { RunInMemory = true};
           
            store.Initialize();

            storage = new RavenSubscriptionStorage { Store = store};
            storage.Init();
        }

    }
}