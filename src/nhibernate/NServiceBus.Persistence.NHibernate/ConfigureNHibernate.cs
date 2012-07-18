namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Logging;
    using global::NHibernate.Mapping.ByCode;
    using Configuration = global::NHibernate.Cfg.Configuration;

    /// <summary>
    /// Helper class to configure NHibernate persisters.
    /// </summary>
    public static class ConfigureNHibernate
    {
        private const string Message =
            @"
To run NServiceBus with NHibernate you need to at least specify the database connectionstring and a dialect.
Here is an example of what is required:
  <appSettings>
    <!-- dialect is the only required NHibernate property -->
    <add key=""NServiceBus/Persistence/NHibernate/dialect"" value=""NHibernate.Dialect.MsSql2008Dialect""/>

    <!-- other optional settings examples -->
    <add key=""NServiceBus/Persistence/NHibernate/connection.provider"" value=""NHibernate.Connection.DriverConnectionProvider""/>
    <add key=""NServiceBus/Persistence/NHibernate/connection.driver_class"" value=""NHibernate.Driver.Sql2008ClientDriver""/>
    <!-- For more setting see http://www.nhforge.org/doc/nh/en/#configuration-hibernatejdbc and http://www.nhforge.org/doc/nh/en/#configuration-optional -->
  </appSettings>
  
  <connectionStrings>
    <!-- Default connection string for all persisters -->
    <add name=""NServiceBus/Persistence/NHibernate"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True"" />
    
    <!-- Optional overrides per persister -->
    <add name=""NServiceBus/Persistence/NHibernate/Timeout"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=timeout;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Saga"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=saga;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Subscription"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=subscription;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Gateway"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=gateway;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Distributor"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=distributor;Integrated Security=True"" />
  </connectionStrings>";

        static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureNHibernate));
        static readonly Regex PropertyRetrievalRegex = new Regex(@"NServiceBus/Persistence/NHibernate/([\W\w]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static ConnectionStringSettingsCollection connectionStringSettingsCollection;

        static ConfigureNHibernate()
        {
            Init();
        }

        public static void Init()
        {
            connectionStringSettingsCollection = NHibernateSettingRetriever.ConnectionStrings() ??
                                                 new ConnectionStringSettingsCollection();

            var defaultConnectionString = GetConnectionStringOrNull("NServiceBus/Persistence/NHibernate");
            var configurationProperties = new Dictionary<string, string>();

            var appSettingsSection = NHibernateSettingRetriever.AppSettings() ?? new NameValueCollection();
            foreach (string appSetting in appSettingsSection)
            {
                var match = PropertyRetrievalRegex.Match(appSetting);
                if (match.Success)
                {
                    configurationProperties.Add(match.Groups[1].Value, appSettingsSection[appSetting]);
                }
            }
            if (!String.IsNullOrEmpty(defaultConnectionString))
            {
                configurationProperties.Add("connection.connection_string", defaultConnectionString);
            }

            TimeoutPersisterProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                                  "NServiceBus/Persistence/NHibernate/Timeout");
            SubscriptionStorageProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                                     "NServiceBus/Persistence/NHibernate/Subscription");
            SagaPersisterProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                               "NServiceBus/Persistence/NHibernate/Saga");
            GatewayPersisterProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                                  "NServiceBus/Persistence/NHibernate/Gateway");
            DistributorPersisterProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                                      "NServiceBus/Persistence/NHibernate/Distributor");
        }

        /// <summary>
        /// Timeout persister NHibernate properties.
        /// </summary>
        public static IDictionary<string, string> TimeoutPersisterProperties { get; private set; }

        /// <summary>
        /// Subscription persister NHibernate properties.
        /// </summary>
        public static IDictionary<string, string> SubscriptionStorageProperties { get; private set; }

        /// <summary>
        /// Saga persister NHibernate properties.
        /// </summary>
        public static IDictionary<string, string> SagaPersisterProperties { get; private set; }

        /// <summary>
        /// Gateway persister NHibernate properties.
        /// </summary>
        public static IDictionary<string, string> GatewayPersisterProperties { get; private set; }

        /// <summary>
        /// Distributor persister NHibernate properties.
        /// </summary>
        public static IDictionary<string, string> DistributorPersisterProperties { get; private set; }

        /// <summary>
        /// Adds T mapping to <param name="configuration">Configuration</param>.
        /// </summary>
        /// <typeparam name="T">The mapping class.</typeparam>
        /// <param name="configuration">The existing <see cref="Configuration"/>.</param>
        public static void AddMappings<T>(Configuration configuration) where T : IConformistHoldersProvider, new()
        {
            var mapper = new ModelMapper();
            mapper.AddMapping<T>();
            var mappings = mapper.CompileMappingForAllExplicitlyAddedEntities();

            configuration.AddMapping(mappings);
        }

        /// <summary>
        /// Validates minimum required NHibernate properties.
        /// </summary>
        /// <param name="props">Properties to validate.</param>
        public static void ThrowIfRequiredPropertiesAreMissing(IDictionary<string, string> props)
        {
            if (props.ContainsKey("connection.connection_string") && props.ContainsKey("dialect"))
            {
                return;
            }

            string errorMsg =
                @"No NHibernate properties found in your config file ({0}).
{1}";
            throw new InvalidOperationException(String.Format(errorMsg, GetConfigFileIfExists(), Message));
        }

        public static void ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(IDictionary<string, string> properties)
        {
            if (!System.Diagnostics.Debugger.IsAttached || properties.Count != 0) 
                return;

            string warningMsg =
                @"No NHibernate properties found in your config file ({0}). 
We have automatically fall back to use SQLite however this only happens while you are running in Visual Studio.
To run in this mode you need to reference the SQLite assembly, here is the NuGet package you need to install:
PM> Install-Package System.Data.SQLite.{1}
{2}";
            Logger.WarnFormat(warningMsg, GetConfigFileIfExists(), Environment.Is64BitOperatingSystem ? "x64" : "x86", Message);

            properties.Add("dialect", "NHibernate.Dialect.SQLiteDialect");
            properties.Add("connection.connection_string", @"Data Source=.\NServiceBus.sqllite;Version=3;New=True;");
        }

        private static string GetConfigFileIfExists()
        {
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile ?? "App.config";
        }

        private static IDictionary<string, string> OverrideConnectionStringSettingIfNotNull(
            IDictionary<string, string> properties, string name)
        {
            var connectionStringOverride = GetConnectionStringOrNull(name);

            if (String.IsNullOrEmpty(connectionStringOverride))
            {
                return new Dictionary<string, string>(properties);
            }

            var overriddenProperties = new Dictionary<string, string>(properties);
            overriddenProperties["connection.connection_string"] = connectionStringOverride;

            return overriddenProperties;
        }

        private static string GetConnectionStringOrNull(string name)
        {
            var connectionStringSettings = connectionStringSettingsCollection[name];

            if (connectionStringSettings == null)
            {
                return null;
            }

            return connectionStringSettings.ConnectionString;
        }
    }
}
