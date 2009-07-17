using System.IO;
using FluentNHibernate;
using FluentNHibernate.Cfg.Db;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests.Config
{
    [TestFixture]
    public class When_configuring_the_subscription_storage
    {
        readonly IPersistenceConfigurer  persistenceConfig = new SQLiteConfiguration()
                          .UsingFile(Path.GetTempFileName())
                          .ShowSql()
                          .Raw("proxyfactory.factory_class",
                               "NHibernate.ByteCode.LinFu.ProxyFactoryFactory, NHibernate.ByteCode.LinFu");

        [Test]
        public void A_raw_session_source_can_be_used()
        {
            var sessionSource = MockRepository.GenerateStub<ISessionSource>();

            var config = Configure.With()
                .SpringBuilder()
                .NHibernateSubcriptionStorage(sessionSource);

            Assert.AreSame(sessionSource, config.Builder.Build<ISessionSource>());
            
            config.Builder.Build<SubscriptionStorage>();
        }

        [Test]
         public void A_user_specified_persistence_configuration_can_be_used()
        {
            var config = Configure.With()
                .SpringBuilder()
                .NHibernateSubcriptionStorage(persistenceConfig);

            //make sure that the session source is configured
            Assert.IsNotNull( config.Builder.Build<ISessionSource>());
        }

        [Test]
        public void Database_schema_should_be_updated_if_requested()
        {
           var config= Configure.With()
               .SpringBuilder()
               .NHibernateSubcriptionStorage(persistenceConfig, true);

            var sessionSource = config.Builder.Build<ISessionSource>();

            using(var session = sessionSource.CreateSession())
            {
                session.CreateCriteria(typeof (Subscription)).List<Subscription>();
            }

        }

        [Test]
        public void NHibernate_proxy_factory_should_default_to_linfu()
        {
            var sqlLiteConfigWithoutProxySpecified = new SQLiteConfiguration().InMemory();

            Configure.With()
             .SpringBuilder()
             .NHibernateSubcriptionStorage(sqlLiteConfigWithoutProxySpecified, true);
        }


    }
}