namespace NServiceBus.Core.Tests.Persistence.RavenDB
{
    using System;
    using NServiceBus.Persistence.Raven;
    using NUnit.Framework;

    [TestFixture]
    public class When_configuring_persistence_to_use_a_raven_server_instance_using_a_connection_string_and_database : WithRavenDbServer
    {
        Configure config;

        protected override void Initialize(Configure config)
        {
            config.RavenPersistence("Raven", "CustomDatabase");
            this.config = config;
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
            Assert.AreEqual("CustomDatabase", store.DefaultDatabase);
        }


        [Test]
        public void It_should_use_the_default_resourceManager_id_if_not_specified_in_the_string()
        {
            Assert.AreEqual(RavenPersistenceConstants.DefaultResourceManagerId(config), store.ResourceManagerId);
        }
    }

    [TestFixture]
    public class When_configuring_the_raven_saga_persister_with_a_connection_string_that_has_a_default_database_set : WithRavenDbServer
    {
        Configure config;

        protected override void Initialize(Configure config)
        {
            config.RavenPersistence("RavenWithDefaultDBSet");
            this.config = config;
        }

        [Test]
        public void It_should_use_the_default_database_of_the_store()
        {
            Assert.AreEqual("MyDB", store.DefaultDatabase);
        }

        [Test]
        public void It_should_use_the_default_resourceManager_id_if_not_specified_in_the_string()
        {
            Assert.AreEqual(RavenPersistenceConstants.DefaultResourceManagerId(config), store.ResourceManagerId);
        }
    }

    [TestFixture]
    public class When_configuring_the_raven_saga_persister_with_a_connection_string_that_has_a_database_set : WithRavenDbServer
    {
        Configure config;

        protected override void Initialize(Configure config)
        {
            config.RavenPersistence("RavenWithDefaultDBSetUsingDataBase");
            this.config = config;
        }

        [Test]
        public void It_should_use_the_default_database_of_the_store()
        {
            Assert.AreEqual("MyDB", store.DefaultDatabase);
        }

        [Test]
        public void It_should_use_the_default_resourceManager_id_if_not_specified_in_the_string()
        {
            Assert.AreEqual(RavenPersistenceConstants.DefaultResourceManagerId(config), store.ResourceManagerId);
        }
    }

    [TestFixture]
    public class When_configuring_the_raven_saga_persister_with_a_connection_string_that_has_a_resourceManager_set : WithRavenDbServer
    {
        protected override void  Initialize(Configure config)
        {
            config.RavenPersistence("RavenWithRmSet");
        }


        [Test]
        public void It_should_use_the_resourceManager_id_specified_in_the_string()
        {
            Assert.AreEqual(Guid.Parse("2f2c3321-f251-4975-802d-11fc9d9e5e37"), store.ResourceManagerId);
        }
    }
}
