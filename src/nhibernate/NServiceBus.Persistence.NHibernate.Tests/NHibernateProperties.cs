namespace NServiceBus.Persistence.NHibernate.Tests
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using NUnit.Framework;

    [TestFixture]
    public class NHibernateProperties
    {
        private const string connectionString = @"Data Source=nsb;Version=3;New=True;";
        private const string dialect = "NHibernate.Dialect.SQLiteDialect";

        [Test]
        public void Should_assign_default_properties_to_all_persisters()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {"NServiceBus/Persistence/NHibernate/dialect", dialect}
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate", connectionString)
                                                                     };

            ConfigureNHibernate.Init();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", connectionString}
                               };

            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.DistributorPersisterProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.GatewayPersisterProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.SagaPersisterProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.SubscriptionStorageProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.TimeoutPersisterProperties);
        }

        [Test]
        public void Should_assign_overriden_connectionstring_if_specified()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {"NServiceBus/Persistence/NHibernate/dialect", dialect}
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate", connectionString),
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate/Timeout", "timeout_connection_string")
                                                                     };

            ConfigureNHibernate.Init();

            var expectedForTimeout = new Dictionary<string, string>
                                         {
                                             {"dialect", dialect},
                                             {"connection.connection_string", "timeout_connection_string"}
                                         };

            var expectedDefault = new Dictionary<string, string>
                                      {
                                          {"dialect", dialect},
                                          {"connection.connection_string", connectionString}
                                      };

            CollectionAssert.AreEquivalent(expectedDefault, ConfigureNHibernate.DistributorPersisterProperties);
            CollectionAssert.AreEquivalent(expectedDefault, ConfigureNHibernate.GatewayPersisterProperties);
            CollectionAssert.AreEquivalent(expectedDefault, ConfigureNHibernate.SagaPersisterProperties);
            CollectionAssert.AreEquivalent(expectedDefault, ConfigureNHibernate.SubscriptionStorageProperties);
            CollectionAssert.AreEquivalent(expectedForTimeout, ConfigureNHibernate.TimeoutPersisterProperties);
        }

        [Test]
        public void Should_assign_all_optional_properties_to_all_persisters()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {"NServiceBus/Persistence/NHibernate/dialect", dialect},
                                                                   {"NServiceBus/Persistence/NHibernate/connection.provider", "provider"},
                                                                   {"NServiceBus/Persistence/NHibernate/connection.driver_class", "driver_class"},
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate", connectionString)
                                                                     };

            ConfigureNHibernate.Init();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", connectionString},
                                   {"connection.provider", "provider"},
                                   {"connection.driver_class", "driver_class"},
                               };

            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.DistributorPersisterProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.GatewayPersisterProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.SagaPersisterProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.SubscriptionStorageProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.TimeoutPersisterProperties);
        }

        [Test]
        public void Should_skip_settings_that_are_not_for_persistence()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {"NServiceBus/Persistence/NHibernate/dialect", dialect},
                                                                   {"myothersetting1", "provider"},
                                                                   {"myothersetting2", "driver_class"},
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate", connectionString)
                                                                     };

            ConfigureNHibernate.Init();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", connectionString},
                               };

            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.DistributorPersisterProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.GatewayPersisterProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.SagaPersisterProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.SubscriptionStorageProperties);
            CollectionAssert.AreEquivalent(expected, ConfigureNHibernate.TimeoutPersisterProperties);
        }
    }
}