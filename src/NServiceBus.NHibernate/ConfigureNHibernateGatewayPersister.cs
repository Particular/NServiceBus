namespace NServiceBus
{
    using GatewayPersister.NHibernate.Config;
    using NHibernate.Cfg;
    using Persistence.NHibernate;

    /// <summary>
    /// Configuration extensions for the NHibernate Gateway persister
    /// </summary>
    public static class ConfigureNHibernateGatewayPersister
    {
        /// <summary>
        /// Configures NHibernate Gateway Persister.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/ms228154.aspx">&lt;appSettings&gt; config section</a> and <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the minimum configuration:
        /// <code lang="XML" escaped="true">
        ///  <appSettings>
        ///    <!-- optional settings examples -->
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
        ///    <add name="NServiceBus/Persistence/NHibernate/Gateway" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=gateway;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateGatewayPersister(this Configure config)
        {
            ConfigureNHibernate.ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(ConfigureNHibernate.GatewayPersisterProperties);

            var properties = ConfigureNHibernate.GatewayPersisterProperties;
            var configuration = ConfigureNHibernate.CreateConfigurationWith(properties);

            return config.UseNHibernateGatewayPersisterInternal(configuration);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        /// <returns>The configuration object</returns>
        public static Configure UseNHibernateGatewayPersister(this Configure config, Configuration configuration)
        {
            foreach (var property in configuration.Properties)
            {
                ConfigureNHibernate.GatewayPersisterProperties[property.Key] = property.Value;
            }

            return config.UseNHibernateGatewayPersisterInternal(configuration);
        }

        /// <summary>
        /// Disables the automatic creation of the database schema.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure DisableNHibernateGatewayPersisterInstall(this Configure config)
        {
            GatewayPersister.NHibernate.Installer.Installer.RunInstaller = false;
            return config;
        }

        private static Configure UseNHibernateGatewayPersisterInternal(this Configure config, Configuration configuration)
        {
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(configuration.Properties);

            GatewayPersister.NHibernate.Installer.Installer.RunInstaller = true;

            ConfigureNHibernate.AddMappings<GatewayMessageMap>(configuration);

            config.Configurer.ConfigureComponent<GatewayPersister.NHibernate.GatewayPersister>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory());

            return config.RunGateway();
        }
    }
}