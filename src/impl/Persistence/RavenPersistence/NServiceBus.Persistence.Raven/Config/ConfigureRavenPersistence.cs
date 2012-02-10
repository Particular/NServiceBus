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
        public static Configure RavenPersistence(this Configure config)
        {
            var connectionStringEntry = ConfigurationManager.ConnectionStrings["NServiceBus.Persistence"];

            //use exisiting config if we can find one
            if (connectionStringEntry != null)
                return RavenPersistence(config, "NServiceBus.Persistence");

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

            var connectionStringEntry = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionStringEntry == null)
                throw new ConfigurationErrorsException(string.Format("No connection string named {0} was found", connectionStringName));

            string database = null;

            if (!connectionStringEntry.ConnectionString.Contains("DefaultDatabase"))
                database = databaseNamingConvention();


            return RavenPersistence(config, connectionStringName, database);
        }

        public static Configure RavenPersistence(this Configure config, string connectionStringName, string database)
        {
            var store = new DocumentStore
            {
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
                ConnectionStringName = connectionStringName,
            };

            if (!string.IsNullOrEmpty(database))
                store.DefaultDatabase = database;

            return RavenPersistence(config, store);
        }

        static Configure RavenPersistence(this Configure config, IDocumentStore store)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (store == null) throw new ArgumentNullException("store");

            var conventions = new RavenConventions();

            store.Conventions.FindTypeTagName = tagNameConvention ?? conventions.FindTypeTagName;

            store.Initialize();

            config.Configurer.RegisterSingleton<IDocumentStore>(store);

            config.Configurer.ConfigureComponent<RavenSessionFactory>(DependencyLifecycle.InstancePerUnitOfWork);
            config.Configurer.ConfigureComponent<RavenUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork);
            
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