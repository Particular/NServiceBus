namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::NHibernate.Cfg;
    using global::NHibernate.Cfg.ConfigurationSchema;
    using global::NHibernate.Mapping.ByCode;
    using Logging;
    using Configuration = global::NHibernate.Cfg.Configuration;
    using Environment = global::NHibernate.Cfg.Environment;

    /// <summary>
    /// Helper class to configure NHibernate persisters.
    /// </summary>
    public static class ConfigureNHibernate
    {
        private const string Message =
            @"
To run NServiceBus with NHibernate you need to at least specify the database connectionstring.
Here is an example of what is required:
  <appSettings>
    <!-- dialect is defaulted to MsSql2008Dialect, if needed change accordingly -->
    <add key=""NServiceBus/Persistence/NHibernate/dialect"" value=""NHibernate.Dialect.{your dialect}""/>

    <!-- other optional settings examples -->
    <add key=""NServiceBus/Persistence/NHibernate/connection.provider"" value=""NHibernate.Connection.DriverConnectionProvider""/>
    <add key=""NServiceBus/Persistence/NHibernate/connection.driver_class"" value=""NHibernate.Driver.Sql2008ClientDriver""/>
    <!-- For more setting see http://www.nhforge.org/doc/nh/en/#configuration-hibernatejdbc and http://www.nhforge.org/doc/nh/en/#configuration-optional -->
  </appSettings>
  
  <connectionStrings>
    <!-- Default connection string for all Nhibernate/Sql persisters -->
    <add name=""NServiceBus/Persistence"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True"" />
    
    <!-- Optional overrides per persister -->
    <add name=""NServiceBus/Persistence/NHibernate/Timeout"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=timeout;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Saga"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=sagas;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Subscription"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=subscription;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Gateway"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=gateway;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Deduplication"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=gateway;Integrated Security=True"" />
    <add name=""NServiceBus/Persistence/NHibernate/Distributor"" connectionString=""Data Source=.\SQLEXPRESS;Initial Catalog=distributor;Integrated Security=True"" />
  </connectionStrings>";

        static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureNHibernate));
        static readonly Regex PropertyRetrievalRegex = new Regex(@"NServiceBus/Persistence/NHibernate/([\W\w]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static ConnectionStringSettingsCollection connectionStringSettingsCollection;

        public static string DefaultDialect = "NHibernate.Dialect.MsSql2008Dialect";


        static ConfigureNHibernate()
        {
            Init();
        }

        /// <summary>
        /// Initializes the <see cref="ConfigureNHibernate"/> NHibernate properties.
        /// </summary>
        /// <remarks>
        /// Configure NHibernate using the <c>&lt;hibernate-configuration&gt;</c> section
        /// from the application config file, if found, or the file <c>hibernate.cfg.xml</c> if the
        /// <c>&lt;hibernate-configuration&gt;</c> section not include the session-factory configuration.
        /// However those settings can be overwritten by our own configuration settings if specified.
        /// </remarks>
        public static void Init()
        {
            connectionStringSettingsCollection = NHibernateSettingRetriever.ConnectionStrings() ??
                                                 new ConnectionStringSettingsCollection();

            var configuration = CreateNHibernateConfiguration();

            var defaultConnectionString = GetConnectionStringOrNull("NServiceBus/Persistence");
            var configurationProperties = configuration.Properties;

            var appSettingsSection = NHibernateSettingRetriever.AppSettings() ?? new NameValueCollection();
            foreach (string appSetting in appSettingsSection)
            {
                var match = PropertyRetrievalRegex.Match(appSetting);
                if (match.Success)
                {
                    configurationProperties[match.Groups[1].Value] = appSettingsSection[appSetting];
                }
            }
            if (!String.IsNullOrEmpty(defaultConnectionString))
            {
                configurationProperties[Environment.ConnectionString] = defaultConnectionString;
            }

            if (!configurationProperties.ContainsKey(Environment.Dialect))
            {
                configurationProperties[Environment.Dialect] = DefaultDialect;
            }

            TimeoutPersisterProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                                  "NServiceBus/Persistence/NHibernate/Timeout");
            SubscriptionStorageProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                                     "NServiceBus/Persistence/NHibernate/Subscription");
            SagaPersisterProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                               "NServiceBus/Persistence/NHibernate/Saga");
            GatewayPersisterProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                                  "NServiceBus/Persistence/NHibernate/Gateway");
            GatewayDeduplicationProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties,
                                                                                  "NServiceBus/Persistence/NHibernate/Deduplication");
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
        /// Gateway deduplication NHibernate properties.
        /// </summary>
        public static IDictionary<string, string> GatewayDeduplicationProperties { get; private set; }

        /// <summary>
        /// Distributor persister NHibernate properties.
        /// </summary>
        public static IDictionary<string, string> DistributorPersisterProperties { get; private set; }

        /// <summary>
        /// Adds T mapping to <paramref name="configuration"/> .
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
            if ((props.ContainsKey(Environment.ConnectionString) || props.ContainsKey(Environment.ConnectionStringName)))
            {
                return;
            }

            var errorMsg =
                @"No NHibernate properties found in your config file ({0}).
{1}";
            throw new InvalidOperationException(String.Format(errorMsg, GetConfigFileIfExists(), Message));
        }

        /// <summary>
        /// It ensures that in DEBUG mode SqlLite is configured if no other settings are specified.
        /// </summary>
        /// <param name="properties">The properties to use.</param>
        public static void ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(IDictionary<string, string> properties)
        {
            if (!System.Diagnostics.Debugger.IsAttached || properties.Count != 0) 
                return;

            var warningMsg =
                @"No NHibernate properties found in your config file ({0}). 
We have automatically fallen back to use SQLite. However, this only happens while you are running in Visual Studio.
To run in this mode you need to reference the SQLite assembly, here is the NuGet package you need to install:
PM> Install-Package System.Data.SQLite.{1}
{2}";
            Logger.WarnFormat(warningMsg, GetConfigFileIfExists(), System.Environment.Is64BitOperatingSystem ? "x64" : "x86", Message);

            properties.Add("dialect", "NHibernate.Dialect.SQLiteDialect");
            properties.Add("connection.connection_string", @"Data Source=.\NServiceBus.sqllite;Version=3;New=True;");
        }

        /// <summary>
        /// Created and initializes a <see cref="Configuration"/> based on <paramref name="properties"/> specified.
        /// </summary>
        /// <param name="properties">The properties to use.</param>
        /// <returns>A properly initialized <see cref="Configuration"/>.</returns>
        public static Configuration CreateConfigurationWith(IDictionary<string, string> properties)
        {
            return new Configuration().SetProperties(properties);
        }

        private static string GetConfigFileIfExists()
        {
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile ?? "App.config";
        }

        private static Configuration CreateNHibernateConfiguration()
        {
            var configuration = new Configuration();
            var hc = ConfigurationManager.GetSection(CfgXmlHelper.CfgSectionName) as IHibernateConfiguration;
            if (hc != null && hc.SessionFactory != null)
            {
                configuration = configuration.Configure();
            }
            else if (File.Exists(GetDefaultConfigurationFilePath()))
            {
                configuration = configuration.Configure();
            }
            return configuration;
        }

        private static string GetDefaultConfigurationFilePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Note RelativeSearchPath can be null even if the doc say something else; don't remove the check
            var searchPath = AppDomain.CurrentDomain.RelativeSearchPath ?? string.Empty;

            var relativeSearchPath = searchPath.Split(';').First();
            var binPath = Path.Combine(baseDir, relativeSearchPath);
            return Path.Combine(binPath, Configuration.DefaultHibernateCfgFileName);
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
            overriddenProperties[Environment.ConnectionString] = connectionStringOverride;

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
