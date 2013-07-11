namespace NServiceBus.Core.Tests.Persistence.RavenDB
{
    using System.Globalization;
    using NUnit.Framework;
    using Utils;

    [TestFixture]
    public class When_configuring_persistence_to_use_a_raven_server_instance_with_the_default_settings : WithRavenDbServer
    {
        [Test]
        public void It_should_configure_the_document_store_to_use_the_default_url()
        {
            var port = RegistryReader<int>.Read("RavenPort", 8080);
            
            var ravenUrl = string.Format("http://localhost:{0}", port.ToString(CultureInfo.InvariantCulture));
            
            Assert.AreEqual(ravenUrl, store.Url);
        }

        [Test]
        public void It_should_configure_the_document_store_with_MaxNumberOfRequestsPerSession()
        {
            Assert.AreEqual(100, store.Conventions.MaxNumberOfRequestsPerSession);
        }

        [Test]
        public void It_should_configure_the_document_store_to_use_the_calling_assembly_name_as_the_database()
        {
            Assert.AreEqual("UnitTests", store.DefaultDatabase);
        }
    }
}
