using System;
using System.Reflection;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus.Persistence.Raven.Config
{
    using global::Raven.Client.Embedded;

    public static class ConfigureRavenPersistence
    {
        public static Configure EmbeddedRavenPersistence(this Configure config)
        {
            var store = new EmbeddableDocumentStore
            {
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
                DataDirectory = RavenPersistenceConstants.DefaultDataDirectory
            };

            return RavenPersistence(config, store);
        }

        public static Configure RavenPersistence(this Configure config)
        {
            var database = Assembly.GetCallingAssembly()
                .GetName().Name;

            var store = new DocumentStore
            {
                Url = RavenPersistenceConstants.DefaultUrl,
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
                DefaultDatabase = database
            };

            return RavenPersistence(config, store);
        }

        public static Configure RavenPersistence(this Configure config, string connectionStringName)
        {
            var database = Assembly.GetCallingAssembly()
                .GetName().Name;

            return RavenPersistence(config, connectionStringName, database);
        }

        public static Configure RavenPersistence(this Configure config, string connectionStringName, string database)
        {
            var store = new DocumentStore
            {
                ConnectionStringName = connectionStringName,
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
                DefaultDatabase = database
            };

            return RavenPersistence(config, store);
        }

        static Configure RavenPersistence(this Configure config, IDocumentStore store)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (store == null) throw new ArgumentNullException("store");

            store.Initialize();

            config.Configurer.RegisterSingleton<IDocumentStore>(store);

            return config;
        }
    }
}