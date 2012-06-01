using NUnit.Framework;
using Raven.Client;

namespace NServiceBus.Unicast.Subscriptions.Raven.Tests
{
    using global::Raven.Client.Embedded;

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