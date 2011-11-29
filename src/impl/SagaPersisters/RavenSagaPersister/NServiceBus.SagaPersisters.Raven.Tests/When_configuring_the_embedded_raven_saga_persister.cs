using NUnit.Framework;
using Raven.Storage.Esent;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    using global::Raven.Client.Embedded;

    [TestFixture]
    public class When_configuring_the_embedded_raven_saga_persister
    {
        RavenSagaPersister persister;
        TransactionalStorage storage;

        [TestFixtureSetUp]
        public void SetUp()
        {
            var config = Configure.With(new[] { GetType().Assembly })
                    .DefineEndpointName("UnitTests")
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
