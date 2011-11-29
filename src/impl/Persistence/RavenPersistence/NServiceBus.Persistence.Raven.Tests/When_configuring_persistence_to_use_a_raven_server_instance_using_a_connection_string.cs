using NServiceBus.Persistence.Raven.Config;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus.Persistence.Raven.Tests
{
    [TestFixture]
    public class When_configuring_persistence_to_use_a_raven_server_instance_using_a_connection_string : WithRavenDbServer
    {
        string connectionStringName;
        IDocumentStore store;

        [TestFixtureSetUp]
        public void SetUp()
        {
            using (var server = GetNewServer())
            {
                connectionStringName = "Raven";

                var config = Configure.With(new[] { GetType().Assembly })
                    .DefineEndpointName("UnitTests")
                    .DefaultBuilder()
                    .RavenPersistence(connectionStringName);

                store = config.Builder.Build<IDocumentStore>();
            }
        }

        [Test]
        public void It_should_use_a_document_store()
        {
            Assert.IsNotNull(store);
        }

        [Test]
        public void It_should_configure_the_document_store_to_use_the_connection_string()
        {
            var s = store as DocumentStore;
            Assert.AreEqual(connectionStringName, s.ConnectionStringName);
        }
    }
}