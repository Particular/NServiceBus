namespace NServiceBus
{
    using System;
    using System.Configuration;
    using System.Net;
    using System.Text;
    using Logging;
    using Persistence.Raven;
    using Raven.Abstractions.Data;
    using Raven.Abstractions.Extensions;
    using Raven.Client;
    using Raven.Client.Document;
    using ILogManager = Raven.Abstractions.Logging.ILogManager;

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
        ///  <connectionStrings>
        ///    <!-- Default connection string name -->
        ///    <add name="NServiceBus/Persistence" connectionString="Url=http://localhost:8080" />
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
            {
                return RavenPersistenceWithConnectionString(config, connectionStringEntry.ConnectionString, null);
            }

            var store = new DocumentStore
            {
                Url = RavenPersistenceConstants.DefaultUrl,
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
                DefaultDatabase = Configure.EndpointName,
            };

            return InternalRavenPersistence(config, store);
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
        /// <param name="getConnectionString">Specifies a callback to call to retrieve the connectionstring to use.</param>
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
        /// <param name="getConnectionString">Specifies a callback to call to retrieve the connectionstring to use.</param>
        /// <param name="database">The database name to use.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenPersistence(this Configure config, Func<string> getConnectionString, string database)
        {
            var connectionString = GetRavenConnectionString(getConnectionString);
            return RavenPersistenceWithConnectionString(config, connectionString, database);
        }

        /// <summary>
        /// Configures RavenDB as the default persistence.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="documentStore">An <see cref="IDocumentStore"/>.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenPersistence(this Configure config, IDocumentStore documentStore)
        {
            return config.InternalRavenPersistence(() => new StoreAccessor(documentStore));
        }

        /// <summary>
        /// The <paramref name="callback"/> is called for further customising the <see cref="IDocumentStore"/>.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="callback">This callback allows to further customise/override default settings.</param>
        /// <returns>The configuration object.</returns>
        public static Configure CustomiseRavenPersistence(this Configure config, Action<IDocumentStore> callback)
        {
            customisationCallback = callback;

            return config;
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

        static Configure InternalRavenPersistence(this Configure config, IDocumentStore documentStore)
        {
            return config.InternalRavenPersistence(() =>
            {
                documentStore.Conventions.FindTypeTagName = RavenConventions.FindTypeTagName;

                documentStore.Conventions.MaxNumberOfRequestsPerSession = 100;

                if (customisationCallback != null)
                {
                    customisationCallback(documentStore);
                }

                WarnUserIfRavenDatabaseIsNotReachable(documentStore);

                return new StoreAccessor(documentStore);
            });
        }

        static Configure InternalRavenPersistence(this Configure config, Func<StoreAccessor> factory)
        {
            config.Configurer.ConfigureComponent(factory, DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenSessionFactory>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenUnitOfWork>(DependencyLifecycle.InstancePerCall);

            Raven.Abstractions.Logging.LogManager.CurrentLogManager = new NoOpLogManager();

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
                store.Url = RavenPersistenceConstants.DefaultUrl;
                store.ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId;
            }

            if (database == null)
            {
                database = Configure.EndpointName;
            }
            else
            {
                store.DefaultDatabase = database;
            }

            if (store.DefaultDatabase == null)
            {
                store.DefaultDatabase = database;
            }

            return InternalRavenPersistence(config, store);
        }

        static void WarnUserIfRavenDatabaseIsNotReachable(IDocumentStore store)
        {
            try
            {
                store.Initialize();

                if (store.Url == null)
                {
                    return;
                }

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

        static void ShowUncontactableRavenWarning(IDocumentStore store)
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
        /// Allows to override the default RavenDB database naming convention.
        /// </summary>
        /// <param name="convention">The mapping convention to use instead.</param>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(Message = "If you need to customise Raven database naming convention, you can either initialise Raven using config.RavenPersistence(IDocumentStore documentStore) or use config.CustomiseRavenPersistence(Action<IDocumentStore> callback).", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]        
        public static Configure DefineRavenDatabaseNamingConvention(this Configure config, Func<string> convention)
        {
            return config;
        }

        /// <summary>
        /// Allows to override the default RavenDB tag name convention for the specified type.
        /// </summary>
        /// <param name="convention">The method referenced by a Func delegate for finding the tag name for the specified type.</param>
        [ObsoleteEx(Message = "If you need to customise Raven FindTypeTagName convention, you can either initialise Raven using config.RavenPersistence(IDocumentStore documentStore) or use config.CustomiseRavenPersistence(Action<IDocumentStore> callback).", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static void DefineRavenTagNameConvention(Func<Type, string> convention)
        {
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
        [ObsoleteEx(Message = "If you need to disable Raven request compression, you can either initialise Raven using config.RavenPersistence(IDocumentStore documentStore) or use config.CustomiseRavenPersistence(Action<IDocumentStore> callback).", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DisableRequestCompression(this Configure config)
        {
            return config;
        }

        /// <summary>
        /// Enables the Raven compression.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(Message = "RequestCompression is on by default from v4.0.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure EnableRequestCompression(this Configure config)
        {
            return config;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureRavenPersistence));
        static Action<IDocumentStore> customisationCallback = store => {};

        class NoOpLogManager : ILogManager
        {
            public Raven.Abstractions.Logging.ILog GetLogger(string name)
            {
                return new Raven.Abstractions.Logging.LogManager.NoOpLogger();
            }

            public IDisposable OpenNestedConext(string message)
            {
                return new DisposableAction(() => { });
            }

            public IDisposable OpenMappedContext(string key, string value)
            {
                return new DisposableAction(() => { });
            }
        }
    }
}