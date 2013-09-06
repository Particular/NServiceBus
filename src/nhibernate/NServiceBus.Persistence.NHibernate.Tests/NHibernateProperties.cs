namespace NServiceBus.Persistence.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class NHibernateProperties
    {
        private const string connectionString = @"Data Source=nsb;Version=3;New=True;";
        private const string dialect = "NHibernate.Dialect.SQLiteDialect";

        [Test]
        public void Should_assign_default_properties_to_all_persisters()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection { };
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
                };

            ConfigureNHibernate.Init();

            var expected = new Dictionary<string, string>
                {
                     {"dialect", ConfigureNHibernate.DefaultDialect},
                     {"connection.connection_string", connectionString}
                   
                };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.DistributorPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.GatewayPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SagaPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SubscriptionStorageProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.TimeoutPersisterProperties);
        }

        [Test]
        public void Should_assign_overridden_connectionString_if_specified()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection{};
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString),
                    new ConnectionStringSettings("NServiceBus/Persistence/NHibernate/Timeout",
                                                 "timeout_connection_string")
                };

            ConfigureNHibernate.Init();

            var expectedForTimeout = new Dictionary<string, string>
                {
                   {"connection.connection_string", "timeout_connection_string"}
                };

            var expectedDefault = new Dictionary<string, string>
                {
                    {"connection.connection_string", connectionString}
                };

            CollectionAssert.IsSubsetOf(expectedDefault, ConfigureNHibernate.DistributorPersisterProperties);
            CollectionAssert.IsSubsetOf(expectedDefault, ConfigureNHibernate.GatewayPersisterProperties);
            CollectionAssert.IsSubsetOf(expectedDefault, ConfigureNHibernate.SagaPersisterProperties);
            CollectionAssert.IsSubsetOf(expectedDefault, ConfigureNHibernate.SubscriptionStorageProperties);
            CollectionAssert.IsSubsetOf(expectedForTimeout, ConfigureNHibernate.TimeoutPersisterProperties);
        }

        [Test]
        public void Should_assign_all_optional_properties_to_all_persisters()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                {
                    {"NServiceBus/Persistence/NHibernate/connection.provider", "provider"},
                    {"NServiceBus/Persistence/NHibernate/connection.driver_class", "driver_class"},
                };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
                };

            ConfigureNHibernate.Init();

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", connectionString},
                    {"connection.provider", "provider"},
                    {"connection.driver_class", "driver_class"},
                };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.DistributorPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.GatewayPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SagaPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SubscriptionStorageProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.TimeoutPersisterProperties);
        }

        [Test]
        public void Should_skip_settings_that_are_not_for_persistence()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                {
                    {"myothersetting1", "provider"},
                    {"myothersetting2", "driver_class"},
                };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
                };

            ConfigureNHibernate.Init();

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", connectionString},
                };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.DistributorPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.GatewayPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SagaPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SubscriptionStorageProperties);
            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.TimeoutPersisterProperties);
        }

        [Test]
        public void Should_read_settings_from_hibernate_configuration_config_section_if_available()
        {
            var appDomain = AppDomain.CreateDomain("Testing", AppDomain.CurrentDomain.Evidence,
                                                   new AppDomainSetup
                                                       {
                                                           ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                           ConfigurationFile = "Testing.config"
                                                       });
            
            var worker = (Worker)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof (Worker).FullName);
            var result = worker.Execute();
            AppDomain.Unload(appDomain);

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", @"Testing"},
                };
            
            CollectionAssert.IsSubsetOf(expected, result);
        }

        [Test]
        public void Should_read_settings_from_hibernate_cfg_xml_if_available()
        {
            var appDomain = AppDomain.CreateDomain("Testing", AppDomain.CurrentDomain.Evidence,
                                                   new AppDomainSetup
                                                   {
                                                       ApplicationBase =
                                                           AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   });

            var worker = (Worker)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(Worker).FullName);
            var result = worker.Execute();
            AppDomain.Unload(appDomain);

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", @"Testing2"},
                };

            CollectionAssert.IsSubsetOf(expected, result);
        }

        [Test]
        public void Our_settings_should_take_precedence_over_settings_from_hibernate_cfg_xml_if_available()
        {
            var appDomain = AppDomain.CreateDomain("Testing", AppDomain.CurrentDomain.Evidence,
                                                   new AppDomainSetup
                                                   {
                                                       ApplicationBase =
                                                           AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   });

            var worker = (Worker2)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(Worker2).FullName);
            var result = worker.Execute();
            AppDomain.Unload(appDomain);

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", "specified"},
                };

            CollectionAssert.IsSubsetOf(expected, result);
        }

        [Test]
        public void Our_settings_should_take_precedence_over_settings_from_hibernate_configuration_config_section_if_available()
        {
            var appDomain = AppDomain.CreateDomain("Testing", AppDomain.CurrentDomain.Evidence,
                                                   new AppDomainSetup
                                                   {
                                                       ConfigurationFile = "Testing.config",
                                                       ApplicationBase =
                                                           AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   });

            var worker = (Worker2)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(Worker2).FullName);
            var result = worker.Execute();
            AppDomain.Unload(appDomain);

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", "specified"},
                };

            CollectionAssert.IsSubsetOf(expected, result);
        }

        public class Worker2 : MarshalByRefObject
        {
            public IDictionary<string, string> Execute()
            {
                NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", "specified")
                };

                ConfigureNHibernate.Init();
                var configuration =
                    ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.DistributorPersisterProperties);

                return configuration.Properties;
            }
        }

        public class Worker : MarshalByRefObject
        {
            public IDictionary<string, string> Execute()
            {

                ConfigureNHibernate.Init();
                var configuration =
                    ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.DistributorPersisterProperties);

                return configuration.Properties;
            }
        }
    }
}