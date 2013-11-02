namespace NServiceBus.Core.Tests.Persistence.RavenDB
{
    using System;
    using NServiceBus.Persistence.Raven;
    using NUnit.Framework;

    [TestFixture]
    public class When_configuring_persistence_to_use_a_raven_server_instance_using_a_connection_string_lambda_and_database : WithRavenDbServer
    {
        Func<string> connectionStringFunc;
        string database;

        protected override void Initialize(Configure config)
        {
            connectionStringFunc = () => "Url = http://localhost:8080; DefaultDatabase=MyDB";
            database = "CustomDatabase";

            config.RavenPersistence(connectionStringFunc, database);
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
        }

        [Test]
        public void It_should_configure_the_document_store_to_use_the_database()
        {
            Assert.AreEqual(database, store.DefaultDatabase);
        }

        [Test]
        public void It_should_use_the_default_resourceManager_id_if_not_specified_in_the_string()
        {
            Assert.AreEqual(RavenPersistenceConstants.DefaultResourceManagerId, store.ResourceManagerId);
        }
    }
}
