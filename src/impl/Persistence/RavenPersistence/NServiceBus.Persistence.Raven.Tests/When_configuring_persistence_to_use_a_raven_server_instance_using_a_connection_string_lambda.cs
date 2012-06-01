using System;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus.Persistence.Raven.Tests
{
    [TestFixture]
    public class When_configuring_persistence_to_use_a_raven_server_instance_using_a_connection_string_lambda : WithRavenDbServer
    {
        Func<string> connectionStringFunc;
        IDocumentStore store;

        [TestFixtureSetUp]
        public void SetUp()
        {
            connectionStringFunc = () => "Url = http://localhost:8080";

            var config = Configure.With(new[] { GetType().Assembly })
                .DefineEndpointName("UnitTests")
                .DefaultBuilder()
                .RavenPersistence(connectionStringFunc);

            store = config.Builder.Build<IDocumentStore>();
        }

        [Test]
        public void It_should_use_a_document_store()
        {
            Assert.IsNotNull(store);
        }

        [Test]
        public void It_should_configure_the_document_store_with_the_connection_string_lambda()
        {
            var s = store as DocumentStore;
            Assert.AreEqual("http://localhost:8080", s.Url);
        }
    }
}