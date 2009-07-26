using System.IO;
using FluentNHibernate;
using FluentNHibernate.Cfg.Db;
using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;
using NBehave.Spec.NUnit;
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
            config = Configure.With()
          .SpringBuilder()
          .NHibernateSubcriptionStorage();

        }

        [Test]
        public void The_session_source_should_be_registered_as_singleton()
        {

            var sessionSource = config.Builder.Build<ISessionSource>();


            sessionSource.ShouldBeTheSameAs(config.Builder.Build<ISessionSource>());

        }

        
        [Test]
        public void The_storage_should_be_registered_as_singlecall()
        {

            var subscriptionStorage = config.Builder.Build<SubscriptionStorage>();


            subscriptionStorage.ShouldNotBeTheSameAs(config.Builder.Build<SubscriptionStorage>());

        }
        
        [Test]
        public void Database_schema_should_be_updated_as_default()
        {
            var sessionSource = config.Builder.Build<ISessionSource>();

            using (var session = sessionSource.CreateSession())
            {
                session.CreateCriteria(typeof(Subscription)).List<Subscription>();
            }

        }


        [Test]
        public void Persister_should_default_to_sqlite_if_no_config_section_is_found()
        {
            var configSource = MockRepository.GenerateStub<IConfigurationSource>();

            var configWithoutConfigSection = Configure.With()
                                                    .SpringBuilder()
                                                    .CustomConfigurationSource(configSource)
                                                    .NHibernateSubcriptionStorage();

            configWithoutConfigSection.Builder.Build<ISessionSource>();

        }

       
        [Test]
        public void NHibernate_proxy_factory_should_default_to_linfu()
        {
            //will fail if no proxy is set
            config.Builder.Build<ISessionSource>();
        }



    }
}