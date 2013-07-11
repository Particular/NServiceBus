namespace NServiceBus.Core.Tests.Persistence.RavenDB
{
    using NUnit.Framework;

    [TestFixture]
    public class When_configuring_persistence_to_use_a_raven_server_instance_using_a_connection_string_lambda : WithRavenDbServer
    {
        protected override void Initialize(Configure config)
        {
            config.RavenPersistence(() => "Url = http://localhost:8080");
        }

        [Test]
        public void It_should_use_a_document_store()
        {
            Assert.IsNotNull(store);
        }

        [Test]
        public void It_should_configure_the_document_store_with_the_connection_string_lambda()
        {
            Assert.AreEqual("http://localhost:8080", store.Url);
        }
    }
}
