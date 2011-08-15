using NServiceBus.Persistence.Raven.Config;
using NUnit.Framework;
using Raven.Client;

namespace NServiceBus.Persistence.Raven.Tests
{
    using global::Raven.Client.Embedded;
    using global::Raven.Storage.Managed;

    [TestFixture]
    public class When_configuring_persistence_to_use_an_embedded_raven_instance
    {
        TransactionalStorage storage;
        IDocumentStore store;

        [TestFixtureSetUp]
        public void SetUp()
        {
            var config = Configure.With(new[] { GetType().Assembly })
                .DefaultBuilder()
                .EmbeddedRavenPersistence();

            store = config.Builder.Build<IDocumentStore>();
        }

        [Test]
        public void It_should_use_an_embedded_document_store()
        {
            Assert.That(store is EmbeddableDocumentStore);
        }
    }
}
