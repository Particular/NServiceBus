namespace NServiceBus
{
    using Deduplication.NHibernate.Config;
    using global::NHibernate.Cfg;
    using Persistence.NHibernate;

    /// <summary>
    /// Configuration extensions for the NHibernate Gateway deduplication
    /// </summary>
    public static class ConfigureNHibernateGatewayDeduplication
    {
        /// <summary>
        /// Configures NHibernate Gateway deduplication.
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
        ///    <add name="NServiceBus/Persistence/NHibernate/Deduplication" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=gateway;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateGatewayDeduplication(this Configure config)
        {
            ConfigureNHibernate.ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(ConfigureNHibernate.GatewayDeduplicationProperties);

            var properties = ConfigureNHibernate.GatewayDeduplicationProperties;
            var configuration = ConfigureNHibernate.CreateConfigurationWith(properties);

            return config.UseNHibernateGatewayDeduplicationInternal(configuration);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        /// <returns>The configuration object</returns>
        public static Configure UseNHibernateGatewayDeduplication(this Configure config, Configuration configuration)
        {
            foreach (var property in configuration.Properties)
            {
                ConfigureNHibernate.GatewayDeduplicationProperties[property.Key] = property.Value;
            }

            return config.UseNHibernateGatewayDeduplicationInternal(configuration);
        }

        /// <summary>
        /// Disables the automatic creation of the database schema.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure DisableNHibernateGatewayDeduplicationInstall(this Configure config)
        {
            Deduplication.NHibernate.Installer.Installer.RunInstaller = false;
            return config;
        }

        private static Configure UseNHibernateGatewayDeduplicationInternal(this Configure config, Configuration configuration)
        {
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(configuration.Properties);

            Deduplication.NHibernate.Installer.Installer.RunInstaller = true;

            ConfigureNHibernate.AddMappings<DeduplicationMessageMap>(configuration);

            config.Configurer.ConfigureComponent<Deduplication.NHibernate.GatewayDeduplication>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory());

            return config.RunGateway();
        }
    }
}