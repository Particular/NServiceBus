namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Linq;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NUnit.Framework;
    using Persistence.NHibernate;

    [TestFixture]
    public class EnsuringBackwardsCompatability
    {
        private const string connectionString = @"Data Source=.\database.sqlite;Version=3;New=True;";
        private const string dialect = "NHibernate.Dialect.SQLiteDialect";

        [SetUp]
        public void Setup()
        {
            NHibernateSettingRetriever.AppSettings = () => null;
            NHibernateSettingRetriever.ConnectionStrings = () => null;
        }

        [Test]
        public void UseNHibernateSubscriptionPersister_Reads_From_AppSettings_And_ConnectionStrings()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {
                                                                       "NServiceBus/Persistence/NHibernate/dialect",
                                                                       dialect
                                                                       }
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings(
                                                                             "NServiceBus/Persistence/NHibernate",
                                                                             connectionString)
                                                                     };

            ConfigureNHibernate.Init();

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateSubscriptionPersister();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", connectionString},
                               };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SubscriptionStorageProperties);
        }

        [Test]
        public void DBSubscriptionStorageWithSQLiteAndAutomaticSchemaGeneration_Automatically_Configure_SqlLite()
        {
            ConfigureNHibernate.Init();

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .DBSubcriptionStorageWithSQLiteAndAutomaticSchemaGeneration();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", "Data Source=.\\NServiceBus.Subscriptions.sqlite;Version=3;New=True;"},
                               };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SubscriptionStorageProperties);
        }

        [Test]
        public void UseNHibernateSubscriptionPersister_Reads_From_DBSubscriptionStorageConfig()
        {
            Configure.ConfigurationSource = new FakeConfigurationSource();

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateSubscriptionPersister();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", connectionString},
                               };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SubscriptionStorageProperties);
        }

        [Test]
        public void UseNHibernateSubscriptionPersister_Reads_First_From_DBSubscriptionStorageConfig()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {
                                                                       "NServiceBus/Persistence/NHibernate/dialect",
                                                                       "Shouldn't be this"
                                                                       }
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings(
                                                                             "NServiceBus/Persistence/NHibernate",
                                                                             "Shouldn't be this")
                                                                     };
            ConfigureNHibernate.Init();

            Configure.ConfigurationSource = new FakeConfigurationSource();

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateSubscriptionPersister();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", connectionString},
                               };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SubscriptionStorageProperties);
        }

        class FakeConfigurationSource : IConfigurationSource
        {
            public T GetConfiguration<T>() where T : class, new()
            {
                var config = new DBSubscriptionStorageConfig
                                 {
                                     UpdateSchema = false,
                                     NHibernateProperties = new FakeNHibernatePropertyCollection(),
                                 };


                return config as T;
            }
        }

        sealed class FakeNHibernatePropertyCollection : NHibernatePropertyCollection
        {
            public FakeNHibernatePropertyCollection()
            {
                BaseAdd(new NHibernateProperty {Key = "dialect", Value = dialect});
                BaseAdd(new NHibernateProperty {Key = "connection.connection_string", Value = connectionString});
            }
        }
    }
}