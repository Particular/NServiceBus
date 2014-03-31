namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Config;
    using NHibernate;
    using global::NHibernate.Cfg;
    using Persistence.NHibernate;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.Config.Internal;
    using UnitOfWork.NHibernate;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the NHibernate saga persister.
    /// </summary>
    public static class ConfigureNHibernateSagaPersister
    {
        /// <summary>
        /// Configures NHibernate Saga Persister.
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
        ///    <add name="NServiceBus/Persistence/NHibernate/Saga" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=sagas;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateSagaPersister(this Configure config)
        {
            var configSection = Configure.GetConfigSection<NHibernateSagaPersisterConfig>();

            if (configSection != null)
            {
                if (configSection.NHibernateProperties.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No NHibernate properties found. Please specify NHibernateProperties in your NHibernateSagaPersisterConfig section");
                }

                foreach (var property in configSection.NHibernateProperties.ToProperties())
                {
                    ConfigureNHibernate.SagaPersisterProperties[property.Key] = property.Value;
                }
            }

            ConfigureNHibernate.ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(ConfigureNHibernate.SagaPersisterProperties);

            var properties = ConfigureNHibernate.SagaPersisterProperties;

            return config.UseNHibernateSagaPersisterInternal(ConfigureNHibernate.CreateConfigurationWith(properties),
                                                                configSection == null || configSection.UpdateSchema);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <param name="configuration">The <see cref="Configuration" /> allows the application to specify properties and mapping documents to be used when creating a <see cref="ISessionFactory" />.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        public static Configure UseNHibernateSagaPersister(this Configure config, Configuration configuration)
        {
            foreach (var property in configuration.Properties)
            {
                ConfigureNHibernate.SagaPersisterProperties[property.Key] = property.Value;
            }

            return config.UseNHibernateSagaPersisterInternal(configuration, true);
        }

        static Configure UseNHibernateSagaPersisterInternal(this Configure config, Configuration configuration, bool autoUpdateSchema)
        {
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.SagaPersisterProperties);

            SagaPersisters.NHibernate.Config.Installer.Installer.RunInstaller = autoUpdateSchema;

            var builder = new SessionFactoryBuilder(Configure.TypesToScan);

            var sessionFactory = builder.Build(configuration);

            if (sessionFactory == null)
            {
                throw new InvalidOperationException("Could not create session factory for saga persistence.");
            }

            config.Configurer.ConfigureComponent<UnitOfWorkManager>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, sessionFactory);

            config.Configurer.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);

            return config;
        }

        /// <summary>
        /// Use the NHibernate backed saga persister implementation.
        /// Be aware that this implementation deletes sagas that complete so as not to have the database fill up.
        /// SagaData classes are automatically mapped using Fluent NHibernate Conventions.
        /// </summary>
        [ObsoleteEx(Replacement = "UseNHibernateSagaPersister()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]                                
        public static Configure NHibernateSagaPersister(this Configure config)
        {
            return config.UseNHibernateSagaPersister();
        }

        /// <summary>
        /// Use the NHibernate backed saga persister implementation on top of SQLite.
        /// SagaData classes are automatically mapped using Fluent NHibernate conventions
        /// and there persistence schema is also automatically generated.
        /// </summary>
        [ObsoleteEx(Replacement = "UseNHibernateSagaPersister()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]                                
        public static Configure NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration(this Configure config)
        {
            ConfigureNHibernate.SagaPersisterProperties["dialect"] = "NHibernate.Dialect.SQLiteDialect";
            ConfigureNHibernate.SagaPersisterProperties["connection.connection_string"] = "Data Source=.\\NServiceBus.Sagas.sqlite;Version=3;New=True;";

            var configuration = ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.SagaPersisterProperties);

            return config.UseNHibernateSagaPersisterInternal(configuration, true);
        }


        /// <summary>
        /// Use the NHibernate backed saga persister implementation.
        /// SagaData classes are automatically mapped using Fluent NHibernate conventions
        /// and there persistence schema is automatically generated if requested.
        /// </summary>
        [ObsoleteEx(Replacement = "UseNHibernateSagaPersister(Configuration)", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]                        
        public static Configure NHibernateSagaPersister(this Configure config, IDictionary<string,string> nhibernateProperties,
            bool autoUpdateSchema)
        {
            foreach (var property in nhibernateProperties)
            {
                ConfigureNHibernate.SagaPersisterProperties[property.Key] = property.Value;
            }

            return config.UseNHibernateSagaPersisterInternal(ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.SagaPersisterProperties), autoUpdateSchema);
        }
    }
}
