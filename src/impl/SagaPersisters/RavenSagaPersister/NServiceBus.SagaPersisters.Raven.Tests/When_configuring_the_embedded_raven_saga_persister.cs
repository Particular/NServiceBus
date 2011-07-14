using NServiceBus.SagaPersisters.Raven.Config;
using NUnit.Framework;
using Raven.Client.Client;
using Raven.Storage.Esent;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    [TestFixture]
    public class When_configuring_the_embedded_raven_saga_persister
    {
        RavenSagaPersister persister;
        TransactionalStorage storage;

        [TestFixtureSetUp]
        public void SetUp()
        {
            var config = Configure.With(new[] { GetType().Assembly })
                .DefaultBuilder()
                .Sagas()
                .EmbeddedRavenSagaPersister();

            persister = config.Builder.Build<RavenSagaPersister>();
        }

        [Test]
        public void It_should_use_an_embedded_document_store()
        {
            Assert.That(persister.Store is EmbeddableDocumentStore);
        }
    }
}
