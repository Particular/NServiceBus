namespace NServiceBus
{
    using System;
    using System.Configuration;
    using Unicast.Queuing.Installers;
    using Unicast.Queuing.SQLServer;

    /// <summary>
    /// Default extension methods to configure the Sql Transport
    /// </summary>
    public static class ConfigureSqlServerTransport
    {
        private const string Message =
            @"
To run NServiceBus with Sql Transport you need to specify the database connectionstring.
Here is an example of what is required:
  
  <connectionStrings>
    <!-- Default connection string for all Sql persisters including the Sql Transport -->
    <add name=""NServiceBus/Persistence"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True"" />
  </connectionStrings>";

        /// <summary>
        /// Use Sql for your queuing infrastructure.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the configuration:
        /// <code lang="XML" escaped="true">
        ///  <connectionStrings>
        ///    <!-- Default connection string name -->
        ///    <add name="NServiceBus/Persistence" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="configure">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure SqlServerTransport(this Configure configure)
        {
            string defaultConnectionString = GetConnectionStringOrNull("NServiceBus/Persistence");

            if (defaultConnectionString == null)
            {
                string errorMsg =
                    @"No default connection string found in your config file ({0}) for the Sql Transport.
{1}";
                throw new InvalidOperationException(String.Format(errorMsg, GetConfigFileIfExists(), Message));
            }

            return configure.InternalSqlServerTransport(defaultConnectionString);
        }

        /// <summary>
        /// Use Sql for your queuing infrastructure.
        /// </summary>
        /// <param name="configure">The configuration object.</param>
        /// <param name="connectionStringName">The connectionstring name to use to retrieve the connectionstring from.</param>
        /// <returns>The configuration object.</returns>
        public static Configure SqlServerTransport(this Configure configure, string connectionStringName)
        {
            string defaultConnectionString = GetConnectionStringOrNull(connectionStringName);

            if (defaultConnectionString == null)
            {
                string errorMsg =
                    @"The connection string named ({0}) was not found in your config file ({1}).";
                throw new InvalidOperationException(String.Format(errorMsg, connectionStringName,
                                                                  GetConfigFileIfExists()));
            }

            return configure.InternalSqlServerTransport(defaultConnectionString);
        }

        /// <summary>
        /// Use Sql for your queuing infrastructure.
        /// </summary>
        /// <param name="configure">The configuration object.</param>
        /// <param name="definesConnectionString">Specifies a callback to call to retrieve the connectionstring to use</param>
        /// <returns>The configuration object.</returns>
        public static Configure SqlServerTransport(this Configure configure, Func<string> definesConnectionString)
        {
            return configure.InternalSqlServerTransport(definesConnectionString());
        }

        private static string GetConfigFileIfExists()
        {
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile ?? "App.config";
        }

        private static string GetConnectionStringOrNull(string name)
        {
            ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[name];

            if (connectionStringSettings == null)
            {
                return null;
            }

            return connectionStringSettings.ConnectionString;
        }

        private static Configure InternalSqlServerTransport(this Configure configure, string connectionString)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Sql Transport connection string cannot be empty or null.");
            }

            configure.Configurer.ConfigureComponent<SqlServerQueueCreator>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connectionString);

            configure.Configurer.ConfigureComponent<SqlServerMessageQueue>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connectionString);

            EndpointInputQueueCreator.Enabled = true;

            return configure;
        }
    }
}