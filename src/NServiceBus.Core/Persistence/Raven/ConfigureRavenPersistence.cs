namespace NServiceBus
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Net;
    using System.Text;
    using Config;
    using Gateway.Deduplication;
    using Gateway.Persistence;
    using Logging;
    using Newtonsoft.Json;
    using Persistence.Raven;
    using Raven.Abstractions.Data;
    using Raven.Abstractions.Extensions;
    using Raven.Client;
    using Raven.Client.Document;
    using Saga;
    using Settings;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using ILogManager = Raven.Abstractions.Logging.ILogManager;

    /// <summary>
    /// Extension methods to configure RavenDB persister.
    /// </summary>
    [ObsoleteEx]
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
        /// <param name="connectionStringName">The connection string name to use to retrieve the connection string from.</param>
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
        /// <param name="connectionStringName">The connection string name to use to retrieve the connection string from.</param>
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
        /// <param name="getConnectionString">Specifies a callback to call to retrieve the connection string to use.</param>
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
        /// <param name="getConnectionString">Specifies a callback to call to retrieve the connection string to use.</param>
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
        /// <remarks>This method does not use any of the NServiceBus conventions either specified or out of the box.</remarks>
        /// <param name="config">The configuration object.</param>
        /// <param name="documentStore">An <see cref="IDocumentStore"/>.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenPersistenceWithStore(this Configure config, IDocumentStore documentStore)
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

        [Obsolete]
        public static void RegisterDefaults()
        {
            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.RavenSagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseRavenTimeoutPersister());
            InfrastructureServices.SetDefaultFor<IPersistMessages>(() => Configure.Instance.UseRavenGatewayPersister());
            InfrastructureServices.SetDefaultFor<IDeduplicateMessages>(() => Configure.Instance.UseRavenGatewayDeduplication());
            InfrastructureServices.SetDefaultFor<ISubscriptionStorage>(() => Configure.Instance.RavenSubscriptionStorage());
        }

        static Configure InternalRavenPersistence(this Configure config, DocumentStore documentStore)
        {
            return config.InternalRavenPersistence(() =>
            {
                documentStore.Conventions.FindTypeTagName = RavenConventions.FindTypeTagName;

                documentStore.Conventions.MaxNumberOfRequestsPerSession = 100;

                if (config.Settings.Get<bool>("Transactions.SuppressDistributedTransactions"))
                {
                    documentStore.EnlistInDistributedTransactions = false;
                }

                if (customisationCallback != null)
                {
                    customisationCallback(documentStore);
                }

                VerifyConnectionToRavenDBServer(documentStore);

                return new StoreAccessor(documentStore);
            });
        }

        static Configure InternalRavenPersistence(this Configure config, Func<StoreAccessor> factory)
        {
            config.Configurer.ConfigureComponent(factory, DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenSessionFactory>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork);

            Raven.Abstractions.Logging.LogManager.CurrentLogManager = new NoOpLogManager();

            RavenUserInstaller.RunInstaller = true;

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
                try
                {
                    store.ParseConnectionString(connectionStringValue);
                }
                catch (ArgumentException)
                {
                    throw new ConfigurationErrorsException(String.Format("Raven connectionstring ({0}) could not be parsed. Please ensure the connectionstring is valid, see http://ravendb.net/docs/client-api/connecting-to-a-ravendb-datastore#using-a-connection-string", connectionStringValue));
                }

                var connectionStringParser = ConnectionStringParser<RavenConnectionStringOptions>.FromConnectionString(connectionStringValue);
                connectionStringParser.Parse();
                if (connectionStringParser.ConnectionStringOptions.ResourceManagerId == Guid.Empty)
                {
                    store.ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId;
                }
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

        static void VerifyConnectionToRavenDBServer(IDocumentStore store)
        {
            RavenBuildInfo ravenBuildInfo = null;
            var connectionSuccessful = false;
            Exception exception = null;
            try
            {
                store.Initialize();

                //for embedded servers
                if (store.Url == null)
                {
                    return;
                }

                var request = WebRequest.Create(string.Format("{0}/build/version", store.Url));
                request.Timeout = 2000;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new InvalidOperationException("Call failed - " + response.StatusDescription);

                    using (var stream = response.GetResponseStream())
                    using(var reader = new StreamReader(stream))
                    {
                        ravenBuildInfo = JsonConvert.DeserializeObject<RavenBuildInfo>(reader.ReadToEnd());
                    }

                    connectionSuccessful = true;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
               if (!connectionSuccessful)
            {
                ShowUncontactableRavenWarning(store,exception);
                return;
            }

            if (!ravenBuildInfo.IsVersion2OrHigher())
            {
                throw new InvalidOperationException(string.Format(WrongRavenVersionMessage, ravenBuildInfo));
            }

         
            Logger.InfoFormat("Connection to RavenDB at {0} verified. Detected version: {1}", store.Url, ravenBuildInfo);
        }

        static void ShowUncontactableRavenWarning(IDocumentStore store,Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Raven could not be contacted. We tried to access Raven using the following url: {0}.",
                            store.Url);
            sb.AppendLine();
            sb.AppendFormat("Please ensure that you can open the Raven Studio by navigating to {0}.", store.Url);
            sb.AppendLine();
            sb.AppendLine(
                @"To configure NServiceBus to use a different Raven connection string add a connection string named ""NServiceBus/Persistence"" in your config file, example:");
            sb.AppendLine(
                @"<connectionStrings>
    <add name=""NServiceBus/Persistence"" connectionString=""Url = http://localhost:9090"" />
</connectionStrings>");
sb.AppendLine("Reason: " + exception);

            Logger.Warn(sb.ToString());
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureRavenPersistence));
        static Action<IDocumentStore> customisationCallback = store => { };

        const string WrongRavenVersionMessage =
@"The RavenDB server you have specified is detected to be {0}. NServiceBus requires RavenDB version 2 or higher to operate correctly. Please update your RavenDB server.

Further instructions can be found at:http://particular.net/articles/using-ravendb-in-nservicebus-installing";

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

        class RavenBuildInfo
        {
            public string ProductVersion { get; set; }

            public string BuildVersion { get; set; }

            public bool IsVersion2OrHigher()
            {
                return !string.IsNullOrEmpty(ProductVersion) && !ProductVersion.StartsWith("1");
            }

            public override string ToString()
            {
                return string.Format("Product version: {0}, Build version: {1}", ProductVersion, BuildVersion);
            }
        }
    }
}