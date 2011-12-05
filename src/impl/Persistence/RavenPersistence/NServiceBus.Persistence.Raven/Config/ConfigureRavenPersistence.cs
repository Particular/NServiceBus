using System;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus
{
    using Persistence.Raven;
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
            var store = new DocumentStore
            {
                Url = RavenPersistenceConstants.DefaultUrl,
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
                DefaultDatabase = Configure.EndpointName
            };

            return RavenPersistence(config, store);
        }

        public static Configure RavenPersistence(this Configure config, string connectionStringName)
        {
            return RavenPersistence(config, connectionStringName, Configure.EndpointName);
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