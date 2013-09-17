namespace NServiceBus
{
    using System;
    using Config;
    using NHibernate;
    using NHibernate.Cfg;
    using Persistence.NHibernate;
    using Unicast.Subscriptions.NHibernate;
    using Unicast.Subscriptions.NHibernate.Config;

    /// <summary>
    /// Configuration extensions for the NHibernate subscription storage
    /// </summary>
    public static class ConfigureNHibernateSubscriptionStorage
    {
        /// <summary>
        /// Configures NHibernate Subscription Persister.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/ms228154.aspx">&lt;appSettings&gt; config section</a> and <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the minimum configuration:
        /// <code lang="XML" escaped="true">
        ///  <appSettings>
        ///    <!-- other optional settings examples -->
        ///    <add key="NServiceBus/Persistence/NHibernate/connection.provider" value="NHibernate.Connection.DriverConnectionProvider"/>
        ///    <add key="NServiceBus/Persistence/NHibernate/connection.driver_class" value="NHibernate.Driver.Sql2008ClientDriver"/>
        ///    <!-- For more setting see http://www.nhforge.org/doc/nh/en/#configuration-hibernatejdbc and http://www.nhforge.org/doc/nh/en/#configuration-optional -->
        ///  </appSettings>
        ///  
        ///  <connectionStrings>
        ///    <!-- Default connection string for all persisters -->
        ///    <add name="NServiceBus/Persistence/NHibernate" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True" />
        ///    
        ///    <!-- Optional overrides per persister -->
        ///    <add name="NServiceBus/Persistence/NHibernate/Subscription" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=subscription;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateSubscriptionPersister(this Configure config)
        {
            var configSection = Configure.GetConfigSection<DBSubscriptionStorageConfig>();

            if (configSection != null)
            {
                if (configSection.NHibernateProperties.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No NHibernate properties found. Please specify NHibernateProperties in your DBSubscriptionStorageConfig section");
                }

                foreach (var property in configSection.NHibernateProperties.ToProperties())
                {
                    ConfigureNHibernate.SubscriptionStorageProperties[property.Key] = property.Value;
                }
            }

            ConfigureNHibernate.ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(ConfigureNHibernate.SubscriptionStorageProperties);

            var properties = ConfigureNHibernate.SubscriptionStorageProperties;

            return config.UseNHibernateSubscriptionPersisterInternal(ConfigureNHibernate.CreateConfigurationWith(properties),
                                                                configSection == null || configSection.UpdateSchema);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <param name="configuration">The <see cref="Configuration" /> allows the application to specify properties and mapping documents to be used when creating a <see cref="ISessionFactory" />.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        public static Configure UseNHibernateSubscriptionPersister(this Configure config, Configuration configuration)
        {
            foreach (var property in configuration.Properties)
            {
                ConfigureNHibernate.SubscriptionStorageProperties[property.Key] = property.Value;
            }

            return config.UseNHibernateSubscriptionPersisterInternal(configuration, true);
        }

        /// <summary>
        /// Disables the automatic creation of the database schema.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure DisableNHibernateSubscriptionPersisterInstall(this Configure config)
        {
            Unicast.Subscriptions.NHibernate.Installer.Installer.RunInstaller = false;
            return config;
        }

        static Configure UseNHibernateSubscriptionPersisterInternal(this Configure config, Configuration configuration, bool autoUpdateSchema)
        {
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.SubscriptionStorageProperties);

            Unicast.Subscriptions.NHibernate.Installer.Installer.RunInstaller = autoUpdateSchema;

            ConfigureNHibernate.AddMappings<SubscriptionMap>(configuration);

            var sessionSource = new SubscriptionStorageSessionProvider(configuration.BuildSessionFactory());

            config.Configurer.RegisterSingleton<ISubscriptionStorageSessionProvider>(sessionSource);
            config.Configurer.ConfigureComponent<SubscriptionStorage>(DependencyLifecycle.InstancePerCall);

            return config;
        }

        /// <summary>
        /// Configures the storage with Sqlite as DB and auto generates schema on startup
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        [ObsoleteEx(Replacement = "UseNHibernateSubscriptionPersister()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]                        
        public static Configure DBSubcriptionStorageWithSQLiteAndAutomaticSchemaGeneration(this Configure config)
        {
            ConfigureNHibernate.SubscriptionStorageProperties["dialect"] = "NHibernate.Dialect.SQLiteDialect";
            ConfigureNHibernate.SubscriptionStorageProperties["connection.connection_string"] = "Data Source=.\\NServiceBus.Subscriptions.sqlite;Version=3;New=True;";

            var configuration = ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.SubscriptionStorageProperties);

            return config.UseNHibernateSubscriptionPersisterInternal(configuration, true);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <param name="configuration">The <see cref="Configuration" /> allows the application to specify properties and mapping documents to be used when creating a <see cref="ISessionFactory" />.</param>
        /// <param name="autoUpdateSchema"><value>True</value> to auto update the database schema.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        [ObsoleteEx(Replacement = "UseNHibernateSubscriptionPersister(Configuration)", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]                
        public static Configure DBSubcriptionStorage(this Configure config, Configuration configuration, bool autoUpdateSchema)
        {
            foreach (var property in configuration.Properties)
            {
                ConfigureNHibernate.SubscriptionStorageProperties[property.Key] = property.Value;
            }

            return config.UseNHibernateSubscriptionPersisterInternal(configuration, autoUpdateSchema);
        }

        /// <summary>
        /// Configures DB Subscription Storage.
        /// Database settings are read from custom config section <see cref="DBSubscriptionStorageConfig"/>.
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        [ObsoleteEx(Replacement = "UseNHibernateSubscriptionPersister()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]        
        public static Configure DBSubcriptionStorage(this Configure config)
        {
            return config.UseNHibernateSubscriptionPersister();
        }
    }
}