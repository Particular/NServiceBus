using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    [TestFixture]
    public class When_configuring_the_raven_saga_persister_with_the_default_settings_and_raven_persistence_has_been_configured : WithRavenDbServer
    {
        RavenSagaPersister persister;
        IDocumentStore store;

        [TestFixtureSetUp]
        public void SetUp()
        {
            using (var server = GetNewServer())
            {
                var config = Configure.With(new[] {GetType().Assembly})
                    .DefaultBuilder()
                    .RavenPersistence();

                store = config.Builder.Build<IDocumentStore>();

                config.Sagas().RavenSagaPersister();

                persister = config.Builder.Build<RavenSagaPersister>();
            }
        }

        [Test]
        public void It_should_configure_raven_to_use_the_configured_raven_persistence()
        {
            var actual = persister.Store as DocumentStore;
            Assert.AreSame(store, actual);
        }
    }
}