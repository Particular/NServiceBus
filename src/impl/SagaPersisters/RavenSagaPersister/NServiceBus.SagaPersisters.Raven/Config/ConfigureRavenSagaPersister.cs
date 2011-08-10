using System;
using System.Reflection;
using NServiceBus.ObjectBuilder;
using NServiceBus.Persistence.Raven;
using NServiceBus.Persistence.Raven.Config;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus
{
    using Raven.Client.Embedded;
    using SagaPersisters.Raven;

    public static class ConfigureRavenSagaPersister
    {
        public static Configure EmbeddedRavenSagaPersister(this Configure config)
        {
            var store = new EmbeddableDocumentStore
            {
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
                DataDirectory = RavenPersistenceConstants.DefaultDataDirectory
            };

            store.Initialize();

            return RavenSagaPersister(config, store, "Default");
        }

        public static Configure RavenSagaPersister(this Configure config)
        {
            if (!config.Configurer.HasComponent<IDocumentStore>())
                config.RavenPersistence();

            var store = config.Builder.Build<IDocumentStore>();
            
            var endpoint = Assembly.GetCallingAssembly()
                .GetName().Name;

            return RavenSagaPersister(config, store, endpoint);
        }

        public static Configure RavenSagaPersister(this Configure config, string connectionStringName)
        {
            var endpoint = Assembly.GetCallingAssembly()
                .GetName().Name;

            return RavenSagaPersister(config, connectionStringName, endpoint);
        }

        public static Configure RavenSagaPersister(this Configure config, string connectionStringName, string endpoint)
        {
            var store = new DocumentStore
            {
                ConnectionStringName = connectionStringName,
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId
            };

            store.Initialize();

            return RavenSagaPersister(config, store, endpoint);
        }

        static Configure RavenSagaPersister(this Configure config, IDocumentStore store, string endpoint)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (store == null) throw new ArgumentNullException("store");

            config.Configurer.ConfigureComponent<RavenSagaPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Endpoint, endpoint)
                .ConfigureProperty(x => x.Store, store);

            return config;
        }
    }
}
