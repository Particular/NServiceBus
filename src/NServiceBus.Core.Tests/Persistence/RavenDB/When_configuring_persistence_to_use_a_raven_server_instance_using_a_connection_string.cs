namespace NServiceBus.Core.Tests.Persistence.RavenDB
{
    using NUnit.Framework;

    [TestFixture]
    public class When_configuring_persistence_to_use_a_raven_server_instance_using_a_connection_string : WithRavenDbServer
    {
        protected override void Initialize(Configure config)
        {
            config.RavenPersistence("Raven");
        }

        [Test]
        public void It_should_use_a_document_store()
        {
            Assert.IsNotNull(store);
        }

        [Test]
        public void It_should_configure_the_document_store_with_the_connection_string()
        {
            Assert.AreEqual("http://localhost:8080", store.Url);
            Assert.AreEqual("b5058088-3a5d-4f35-8a64-49b06719d6ef", store.ApiKey);
        }

        [Test]
        public void It_should_configure_the_document_store_to_use_the_calling_assembly_name_as_the_database()
        {
            Assert.AreEqual("UnitTests", store.DefaultDatabase);
        }
    }
}
