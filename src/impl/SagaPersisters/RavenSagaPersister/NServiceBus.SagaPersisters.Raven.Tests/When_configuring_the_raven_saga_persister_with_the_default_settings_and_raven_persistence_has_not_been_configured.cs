using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    [TestFixture]
    public class When_configuring_the_raven_saga_persister_with_the_default_settings_and_raven_persistence_has_not_been_configured : WithRavenDbServer
    {
        RavenSagaPersister persister;
        IDocumentStore store;

        [SetUp]
        public void SetUp()
        {
            using (var server = GetNewServer())
            {
                var config = Configure.With(new[] {GetType().Assembly})
                    .DefineEndpointName("MyEndpoint")
                    .DefaultBuilder()
                    .Sagas().RavenSagaPersister();

                persister = config.Builder.Build<RavenSagaPersister>();

                store = config.Builder.Build<IDocumentStore>();
            }
        }

        [Test]
        public void It_should_configure_raven_to_use_raven_persistence()
        {
            var actual = persister.Store as DocumentStore;
            Assert.AreSame(store, actual);
        }

        [Test]
        public void It_should_configure_to_use_the_endpointname_as_the_database()
        {
            var actual = persister.Store as DocumentStore;
            Assert.AreEqual("MyEndpoint", actual.DefaultDatabase);
        }
    }
}