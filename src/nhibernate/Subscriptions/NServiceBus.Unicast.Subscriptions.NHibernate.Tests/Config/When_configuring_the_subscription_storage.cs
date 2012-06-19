using System;
using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests.Config
{
    [TestFixture]
    public class When_configuring_the_subscription_storage
    {
        private Configure config;

        [SetUp]
        public void SetUp()
        {
            config = Configure.With(new Type[] { })
                                .DefaultBuilder()
                                .DBSubscriptionStorage();

        }

        [Test]
        [Ignore]
        public void The_session_provider_should_be_registered_as_singleton()
        {

            var sessionSource = config.Builder.Build<ISubscriptionStorageSessionProvider>();

            Assert.That(sessionSource, Is.EqualTo(config.Builder.Build<ISubscriptionStorageSessionProvider>()));

           // sessionSource.ShouldBeTheSameAs(config.Builder.Build<ISubscriptionStorageSessionProvider>());

        }


        [Test]
        [Ignore]
        public void The_storage_should_be_registered_as_singlecall()
        {

            var subscriptionStorage = config.Builder.Build<SubscriptionStorage>();

            Assert.That(subscriptionStorage, Is.Not.EqualTo(config.Builder.Build<SubscriptionStorage>()));

            //subscriptionStorage.ShouldNotBeTheSameAs(config.Builder.Build<SubscriptionStorage>());

        }

        [Test]
        [Ignore]
        public void Database_schema_should_be_updated_as_default()
        {
            var sessionSource = config.Builder.Build<ISubscriptionStorageSessionProvider>();

            using (var session = sessionSource.OpenSession())
            {
                session.CreateCriteria(typeof(Subscription)).List<Subscription>();
            }

        }


        [Test]
        [Ignore]
        public void Persister_can_be_configured_to_use_sqlite_if_no_config_section_is_found()
        {
            var configSource = MockRepository.GenerateStub<IConfigurationSource>();

            var configWithoutConfigSection = Configure.With(new Type[]{})
                                                    .DefaultBuilder()
                                                    .CustomConfigurationSource(configSource)
                                                    .DBSubscriptionStorageWithSQLiteAndAutomaticSchemaGeneration();

            configWithoutConfigSection.Builder.Build<ISubscriptionStorageSessionProvider>();

        }

        [Test]
        [Ignore]
        public void NHibernate_proxy_factory_should_default_to_linfu()
        {
            //will fail if no proxy is set
            config.Builder.Build<ISubscriptionStorageSessionProvider>();
        }



    }
}