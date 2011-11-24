using NUnit.Framework;
using Raven.Client.Document;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    [TestFixture]
    public class When_configuring_the_raven_saga_persister_with_a_connection_string_and_database : WithRavenDbServer
    {
        RavenSagaPersister persister;
        string connectionStringName;
        string database;

        [TestFixtureSetUp]
        public void SetUp()
        {
            using (var server = GetNewServer())
            {
                connectionStringName = "Raven";

                var config = Configure.With(new[] {GetType().Assembly})
                    .DefaultBuilder()
                    .Sagas().RavenSagaPersister(connectionStringName);

                persister = config.Builder.Build<RavenSagaPersister>();
            }
        }

        [Test]
        public void It_should_use_a_document_store_configured_with_the_connection_string()
        {
            var store = persister.Store as DocumentStore;
            Assert.AreEqual(connectionStringName, store.ConnectionStringName);
        }
    }
}