namespace NServiceBus
{
    using System;
    using System.Configuration;
    using System.Net;
    using System.Text;
    using Logging;
    using Persistence.Raven;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Document;

    /// <summary>
    /// Extension methods to configure RavenDB persister.
    /// </summary>
    public static class ConfigureRavenPersistence
    {
        /// <summary>
        /// Configures RavenDB as the default persistence.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/ms228154.aspx">&lt;appSettings&gt; config section</a> and <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the configuration:
        /// <code lang="XML" escaped="true">
        ///  <appSettings>
        ///    <!-- Optional overwrites for number of requests that each RavenDB session is allowed to make -->
        ///    <add key="NServiceBus/Persistence/RavenDB/MaxNumberOfRequestsPerSession" value="50"/>
        ///  </appSettings>
        ///  
        ///  <connectionStrings>
        ///    <!-- Default connection string name -->
        ///    <add name="NServiceBus/Persistence" connectionString="http://localhost:8080" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenPersistence(this Configure config)
        {
            if (Configure.Instance.Configurer.HasComponent<RavenSessionFactory>())
            {
                return config;
            }

            var connectionStringEntry = ConfigurationManager.ConnectionStrings["NServiceBus.Persistence"] ?? ConfigurationManager.ConnectionStrings["NServiceBus/Persistence"];

            //use existing config if we can find one
            if (connectionStringEntry != null)
                return RavenPersistenceWithConnectionString(config, connectionStringEntry.ConnectionString, null);

            var store = new DocumentStore
                {
                    Url = RavenPersistenceConstants.DefaultUrl,
                    ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
                    DefaultDatabase = databaseNamingConvention()
                };

            return RavenPersistence(config, store);
        }

        /// <summary>
        /// Configures RavenDB as the default persistence.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="connectionStringName">The connectionstring name to use to retrieve the connectionstring from.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenPersistence(this Configure config, string connectionStringName)
        {
            var connectionStringEntry = GetRavenConnectionString(connectionStringName);
            return RavenPersistenceWithConnectionString(config, connectionStringEntry, null);
        }

        /// <summary>
        /// Configures RavenDB as the default persistence.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="connectionStringName">The connectionstring name to use to retrieve the connectionstring from.</param>
        /// <param name="database">The database name to use.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenPersistence(this Configure config, string connectionStringName, string database)
        {
            var connectionString = GetRavenConnectionString(connectionStringName);
            return RavenPersistenceWithConnectionString(config, connectionString, database);
        }

        /// <summary>
        /// Configures RavenDB as the default persistence.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="getConnectionString">Specifies a callback to call to retrieve the connectionstring to use</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenPersistence(this Configure config, Func<string> getConnectionString)
        {
            var connectionString = GetRavenConnectionString(getConnectionString);
            return RavenPersistenceWithConnectionString(config, connectionString, null);
        }

        /// <summary>
        /// Configures RavenDB as the default persistence.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="getConnectionString">Specifies a callback to call to retrieve the connectionstring to use</param>
        /// <param name="database">The database name to use.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenPersistence(this Configure config, Func<string> getConnectionString, string database)
        {
            var connectionString = GetRavenConnectionString(getConnectionString);
            return RavenPersistenceWithConnectionString(config, connectionString, database);
        }

        /// <summary>
        /// Specifies the mapping to use for when resolving the database name to use for each message.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="convention">The method referenced by a Func delegate for finding the database name for the specified message.</param>
        /// <returns>The configuration object.</returns>
        public static Configure MessageToDatabaseMappingConvention(this Configure config, Func<IMessageContext, string> convention)
        {
            RavenSessionFactory.GetDatabaseName = convention;

            return config;
        }

        static string GetRavenConnectionString(Func<string> getConnectionString)
        {
            var connectionString = getConnectionString();

            if (connectionString == null)
                throw new ConfigurationErrorsException("Cannot configure Raven Persister. No connection string was found");

            return connectionString;
        }

        static string GetRavenConnectionString(string connectionStringName)
        {
            var connectionStringEntry = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionStringEntry == null)
                throw new ConfigurationErrorsException(string.Format("Cannot configure Raven Persister. No connection string named {0} was found",
                                                                     connectionStringName));
            return connectionStringEntry.ConnectionString;
        }

        static Configure RavenPersistenceWithConnectionString(Configure config, string connectionStringValue, string database)
        {
            var store = new DocumentStore();

            if (connectionStringValue != null)
            {
                store.ParseConnectionString(connectionStringValue);

                var connectionStringParser = ConnectionStringParser<RavenConnectionStringOptions>.FromConnectionString(connectionStringValue);
                connectionStringParser.Parse();
                if (connectionStringParser.ConnectionStringOptions.ResourceManagerId == Guid.Empty)
                    store.ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId;
            }
            else
            {
                if (database == null)
                {
                    database = databaseNamingConvention();
                }

                store.Url = RavenPersistenceConstants.DefaultUrl;
                store.ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId;
            }

            if (database != null)
            {
                store.DefaultDatabase = database;
            }

            return RavenPersistence(config, store);
        }

        static Configure RavenPersistence(this Configure config, IDocumentStore store)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (store == null) throw new ArgumentNullException("store");

            var maxNumberOfRequestsPerSession = 100;
            var ravenMaxNumberOfRequestsPerSession = ConfigurationManager.AppSettings["NServiceBus/Persistence/RavenDB/MaxNumberOfRequestsPerSession"];
            if (!String.IsNullOrEmpty(ravenMaxNumberOfRequestsPerSession))
            {
                if (!Int32.TryParse(ravenMaxNumberOfRequestsPerSession, out maxNumberOfRequestsPerSession))
                {
                    throw new ConfigurationErrorsException(string.Format("Cannot configure RavenDB MaxNumberOfRequestsPerSession. Cannot convert value '{0}' in <appSettings> with key 'NServiceBus/Persistence/RavenDB/MaxNumberOfRequestsPerSession' to a numeric value.", ravenMaxNumberOfRequestsPerSession));
                }
            }

            config.Configurer.ConfigureComponent(() =>
                {
                    var conventions = new RavenConventions();

                    store.Conventions.FindTypeTagName = tagNameConvention ?? conventions.FindTypeTagName;

                    WarnUserIfRavenDatabaseIsNotReachable(store);

                    store.Conventions.MaxNumberOfRequestsPerSession = maxNumberOfRequestsPerSession;

                    //We need to turn compression off to make us compatible with Raven616
                    store.JsonRequestFactory.DisableRequestCompression = !enableRequestCompression;
                    
                    if (unsafeAuthenticatedConnectionSharingAndPreAuthenticate)
                    {
                        store.JsonRequestFactory.ConfigureRequest += (sender, e) =>
                            {
                                var httpWebRequest = ((HttpWebRequest) e.Request);
                                httpWebRequest.UnsafeAuthenticatedConnectionSharing = true;
                                httpWebRequest.PreAuthenticate = true;
                            };
                    }
                    return store;
                }, DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenSessionFactory>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenUnitOfWork>(DependencyLifecycle.InstancePerCall);

            return config;
        }

        private static void WarnUserIfRavenDatabaseIsNotReachable(IDocumentStore store)
        {
            try
            {
                store.Initialize();
                var request = WebRequest.Create(string.Format("{0}/build/version", store.Url));
                request.Timeout = 2000;
                using (var response = request.GetResponse())
                {
                    if (response.Headers.Get("Raven-Server-Build") == null)
                    {
                        ShowUncontactableRavenWarning(store);
                    }
                }
            }
            catch (WebException)
            {
                ShowUncontactableRavenWarning(store);
            }
            catch (InvalidOperationException)
            {
                ShowUncontactableRavenWarning(store);
            }
        }

        private static void ShowUncontactableRavenWarning(IDocumentStore store)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Raven could not be contacted. We tried to access Raven using the following url: {0}.",
                            store.Url);
            sb.AppendLine();
            sb.AppendFormat("Please ensure that you can open the Raven Studio by navigating to {0}.", store.Url);
            sb.AppendLine();
            sb.AppendLine(
                @"To configure NServiceBus to use a different Raven connection string add a connection string named ""NServiceBus.Persistence"" in your config file, example:");
            sb.AppendFormat(
                @"<connectionStrings>
    <add name=""NServiceBus.Persistence"" connectionString=""Url = http://localhost:9090"" />
</connectionStrings>");

            Logger.Warn(sb.ToString());
        }

        /// <summary>
        /// Disables RavenDB installation.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(Message = "You don't need to call this since Raven is now installed via powershell cmdlet, see http://nservicebus.com/powershell.aspx", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DisableRavenInstall(this Configure config)
        {
            return config;
        }

        /// <summary>
        /// Installs RavenDB if needed.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(Message = "Use the powershell cmdlet to install Raven instead. See http://nservicebus.com/powershell.aspx", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure InstallRavenIfNeeded(this Configure config)
        {
            return config;
        }

        /// <summary>
        /// Disables the Raven compression.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(Replacement = "DisableRavenRequestCompression()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DisableRequestCompression(this Configure config)
        {
            return config.DisableRavenRequestCompression();
        }
        
        /// <summary>
        /// Disables the Raven compression.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure DisableRavenRequestCompression(this Configure config)
        {
            enableRequestCompression = false;

            return config;
        }

        /// <summary>
        /// Use this setting if you are experiencing error like:
        /// An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full 127.0.0.1:8080
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure EnableRavenRequestsWithUnsafeAuthenticatedConnectionSharingAndPreAuthenticate(this Configure config)
        {
            unsafeAuthenticatedConnectionSharingAndPreAuthenticate = true;

            return config;
        }

        /// <summary>
        /// Allows to override the default RavenDB database naming convention.
        /// </summary>
        /// <param name="convention">The mapping convention to use instead.</param>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure DefineRavenDatabaseNamingConvention(this Configure config, Func<string> convention)
        {
            databaseNamingConvention = convention;

            return config;
        }

        /// <summary>
        /// Allows to override the default RavenDB tag name convention for the specified type.
        /// </summary>
        /// <param name="convention">The method referenced by a Func delegate for finding the tag name for the specified type.</param>
        public static void DefineRavenTagNameConvention(Func<Type, string> convention)
        {
            tagNameConvention = convention;
        }

        static bool unsafeAuthenticatedConnectionSharingAndPreAuthenticate;
        static bool enableRequestCompression = true;
        static Func<string> databaseNamingConvention = () => Configure.EndpointName;
        static Func<Type, string> tagNameConvention;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureRavenPersistence));
    }
}