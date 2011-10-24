using System.IO;
using NUnit.Framework;
using Raven.Client;

namespace NServiceBus.Unicast.Subscriptions.Raven.Tests
{
    using global::Raven.Client.Embedded;

    public class WithRavenSubscriptionStorage
    {
        protected ISubscriptionStorage storage;
        protected IDocumentStore store;
        string path;
        [TestFixtureSetUp]
        public void SetupContext()
        {
            path = Path.GetRandomFileName();
            store = new EmbeddableDocumentStore { RunInMemory = true, DataDirectory = path };
            store.Initialize();

            storage = new RavenSubscriptionStorage { Store = store, Endpoint = "SubscriptionEndpoint"};
            storage.Init();
        }

        [TestFixtureTearDown]
        public void Teardown()
        {
            Directory.Delete(path, true);
        }
    }
}