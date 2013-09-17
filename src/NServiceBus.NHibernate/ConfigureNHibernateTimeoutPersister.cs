namespace NServiceBus
{
    using System;
    using Config;
    using NHibernate.Cfg;
    using Persistence.NHibernate;
    using TimeoutPersisters.NHibernate;
    using TimeoutPersisters.NHibernate.Config;

    /// <summary>
    /// Configuration extensions for the NHibernate Timeouts persister
    /// </summary>
    public static class ConfigureNHibernateTimeoutPersister
    {
        /// <summary>
        /// Configures NHibernate Timeout Persister.
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
        ///    <add name="NServiceBus/Persistence/NHibernate/Timeout" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=timeout;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateTimeoutPersister(this Configure config)
        {
            var configSection = Configure.GetConfigSection<TimeoutPersisterConfig>();

            if (configSection != null)
            {
                if (configSection.NHibernateProperties.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No NHibernate properties found. Please specify NHibernateProperties in your TimeoutPersisterConfig section");
                }

                foreach (var property in configSection.NHibernateProperties.ToProperties())
                {
                    ConfigureNHibernate.TimeoutPersisterProperties[property.Key] = property.Value;
                }
            }

            ConfigureNHibernate.ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(ConfigureNHibernate.TimeoutPersisterProperties);

            var properties = ConfigureNHibernate.TimeoutPersisterProperties;

            return config.UseNHibernateTimeoutPersisterInternal(ConfigureNHibernate.CreateConfigurationWith(properties),
                                                                configSection == null || configSection.UpdateSchema);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration.
        /// Database schema is updated if requested by the user.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        /// <param name="autoUpdateSchema"><value>true</value> to auto update schema</param>
        /// <returns>The configuration object</returns>
        public static Configure UseNHibernateTimeoutPersister(this Configure config, Configuration configuration, bool autoUpdateSchema)
        {
            foreach (var property in configuration.Properties)
            {
                ConfigureNHibernate.TimeoutPersisterProperties[property.Key] = property.Value;
            }

            return config.UseNHibernateTimeoutPersisterInternal(configuration, autoUpdateSchema);
        }

        /// <summary>
        /// Disables the automatic creation of the database schema.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure DisableNHibernateTimeoutPersisterInstall(this Configure config)
        {
            TimeoutPersisters.NHibernate.Installer.Installer.RunInstaller = false;
            return config;
        }

        static Configure UseNHibernateTimeoutPersisterInternal(this Configure config, Configuration configuration, bool autoUpdateSchema)
        {
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.TimeoutPersisterProperties);

            TimeoutPersisters.NHibernate.Installer.Installer.RunInstaller = autoUpdateSchema;
            ConfigureNHibernate.AddMappings<TimeoutEntityMap>(configuration);

            config.Configurer.ConfigureComponent<TimeoutStorage>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory());

            return config;
        }

        /// <summary>
        /// Configures the persister with Sqlite as its database and auto generates schema on startup.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(Replacement = "UseNHibernateTimeoutPersister()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]                        
        public static Configure UseNHibernateTimeoutPersisterWithSQLiteAndAutomaticSchemaGeneration(this Configure config)
        {
            ConfigureNHibernate.TimeoutPersisterProperties["dialect"] = "NHibernate.Dialect.SQLiteDialect";
            ConfigureNHibernate.TimeoutPersisterProperties["connection.connection_string"] = "Data Source=.\\NServiceBus.Timeouts.sqlite;Version=3;New=True;";

            var configuration = ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.TimeoutPersisterProperties);

            return config.UseNHibernateTimeoutPersisterInternal(configuration, true);
        }
    }
}