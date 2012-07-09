using System;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus
{
    using System.Configuration;
    using Persistence.Raven;
    using Persistence.Raven.Installation;

    public static class ConfigureRavenPersistence
    {
        /// <summary>
        /// Configures Raven Persister.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/ms228154.aspx">&lt;appSettings&gt; config section</a> and <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the configuration:
        /// <code lang="XML" escaped="true">
        ///  <appSettings>
        ///    <!-- Optional overrider for number of requests that each RavenDB session is allowed to make -->
        ///    <add key="NServiceBus/Persistence/RavenDB/MaxNumberOfRequestsPerSession" value="50"/>
        ///  </appSettings>
        ///  
        ///  <connectionStrings>
        ///    <!-- Default connection string name -->
        ///    <add name="NServiceBus.Persistence" connectionString="http://localhost:8080" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RavenPersistence(this Configure config)
        {
            var connectionStringEntry = ConfigurationManager.ConnectionStrings["NServiceBus.Persistence"];

            //use exisiting config if we can find one
            if (connectionStringEntry != null)
                return RavenPersistenceWithConnectionString(config, connectionStringEntry.ConnectionString, null);

            var store = new DocumentStore
            {
                Url = RavenPersistenceConstants.DefaultUrl,
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
                DefaultDatabase = databaseNamingConvention()
            };

            //if we have no master node we should have our own local ravendb
            installRavenIfNeeded = string.IsNullOrEmpty(config.GetMasterNode());

            return RavenPersistence(config, store);
        }

        public static Configure RavenPersistence(this Configure config, string connectionStringName)
        {
            var connectionStringEntry = GetRavenConnectionString(connectionStringName);
            return RavenPersistenceWithConnectionString(config, connectionStringEntry, null);
        }
        
        public static Configure RavenPersistence(this Configure config, string connectionStringName, string database)
        {
            var connectionString = GetRavenConnectionString(connectionStringName);
            return RavenPersistenceWithConnectionString(config, connectionString, database);
        }

        public static Configure RavenPersistence(this Configure config, Func<string> getConnectionString)
        {
            var connectionString = GetRavenConnectionString(getConnectionString);
            return RavenPersistenceWithConnectionString(config, connectionString, null);
        }
        
        public static Configure RavenPersistence(this Configure config, Func<string> getConnectionString, string database)
        {
            var connectionString = GetRavenConnectionString(getConnectionString);
            return RavenPersistenceWithConnectionString(config, connectionString, database);
        }

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
            var store = new DocumentStore
            {
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId
            };

            store.ParseConnectionString(connectionStringValue);

            if (!string.IsNullOrEmpty(database))
                store.DefaultDatabase = database;
            else if (!connectionStringValue.Contains("DefaultDatabase"))
                store.DefaultDatabase = databaseNamingConvention();
            
            return RavenPersistence(config, store);
        }

        static Configure RavenPersistence(this Configure config, IDocumentStore store)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (store == null) throw new ArgumentNullException("store");

            var conventions = new RavenConventions();

            store.Conventions.FindTypeTagName = tagNameConvention ?? conventions.FindTypeTagName;
            

            store.Initialize();

            var maxNumberOfRequestsPerSession = 100;
            var ravenMaxNumberOfRequestsPerSession = ConfigurationManager.AppSettings["NServiceBus/Persistence/RavenDB/MaxNumberOfRequestsPerSession"];
            if (!String.IsNullOrEmpty(ravenMaxNumberOfRequestsPerSession))
            {
                if(!Int32.TryParse(ravenMaxNumberOfRequestsPerSession, out maxNumberOfRequestsPerSession))
                    throw new ConfigurationErrorsException(string.Format("Cannot configure RavenDB MaxNumberOfRequestsPerSession. Cannot convert value '{0}' in <appSettings> with key 'NServiceBus/Persistence/RavenDB/MaxNumberOfRequestsPerSession' to a numeric value.", ravenMaxNumberOfRequestsPerSession));
            }
            store.Conventions.MaxNumberOfRequestsPerSession = maxNumberOfRequestsPerSession;

            config.Configurer.RegisterSingleton<IDocumentStore>(store);

            config.Configurer.ConfigureComponent<RavenSessionFactory>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RavenUnitOfWork>(DependencyLifecycle.InstancePerCall);
            
            RavenDBInstaller.InstallEnabled = installRavenIfNeeded && ravenInstallEnabled;

            return config;
        }

        public static Configure DisableRavenInstall(this Configure config)
        {
            ravenInstallEnabled = false;

            return config;
        }
        
        public static Configure InstallRavenIfNeeded(this Configure config)
        {
            installRavenIfNeeded = true;

            return config;
        }
        
        static bool installRavenIfNeeded;


        static bool ravenInstallEnabled = true;


        public static Configure DefineRavenDatabaseNamingConvention(this Configure config, Func<string> convention)
        {
            databaseNamingConvention = convention;

            return config;
        }

        static Func<string> databaseNamingConvention = () => Configure.EndpointName;
        

        public static void DefineRavenTagNameConvention(Func<Type, string> convention)
        {
            tagNameConvention = convention;
        }

        static Func<Type, string> tagNameConvention;
    }
}