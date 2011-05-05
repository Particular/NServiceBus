using NServiceBus.Persistence.Raven.Config;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus.Persistence.Raven.Tests
{
    [TestFixture]
    public class When_configuring_persistence_to_use_a_raven_server_instance_with_the_default_settings : WithRavenDbServer
    {
        DocumentStore store;

        [TestFixtureSetUp]
        public void SetUp()
        {
            using (var server = GetNewServer())
            {
                var config = Configure.With(new[] { GetType().Assembly })
                    .DefaultBuilder()
                    .RavenPersistence();

                store = config.Builder.Build<IDocumentStore>() as DocumentStore;
            }
        }

        [Test]
        public void It_should_configure_the_document_store_to_use_the_default_url()
        {
            Assert.AreEqual("http://localhost:8080", store.Url);
        }

        [Test]
        public void It_should_configure_the_document_store_to_use_the_calling_assembly_name_as_the_database()
        {

            Assert.AreEqual(GetType().Assembly.GetName().Name, store.DefaultDatabase);
        }
    }
}