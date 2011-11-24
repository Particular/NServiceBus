using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.Persistence.Raven.Config;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus
{
    using Unicast.Subscriptions.Raven;
    using global::Raven.Client.Embedded;

    public static class ConfigureRavenSubscriptionStorage
    {
        const string DefaultDataDirectory = @".\SagaPersistence";
        static readonly Guid DefaultResourceManagerId = new Guid("DB7F1F2E-643C-4C20-91AD-B01FF4882A2B");

        public static Configure EmbeddedRavenSubscriptionStorage(this Configure config)
        {
            var store = new EmbeddableDocumentStore { ResourceManagerId = DefaultResourceManagerId, DataDirectory = DefaultDataDirectory };
            store.Initialize();

            return RavenSubscriptionStorage(config, store, Configure.EndpointName);
        }

        public static Configure RavenSubscriptionStorage(this Configure config)
        {
            if (!config.Configurer.HasComponent<IDocumentStore>())
                config.RavenPersistence();

            var store = config.Builder.Build<IDocumentStore>();

            return RavenSubscriptionStorage(config, store, Configure.EndpointName);
        }

        public static Configure RavenSubscriptionStorage(this Configure config, string connectionStringName)
        {
            var store = new DocumentStore { ConnectionStringName = connectionStringName, ResourceManagerId = DefaultResourceManagerId };
            store.Initialize();

            return RavenSubscriptionStorage(config, store, Configure.EndpointName);
        }

        static Configure RavenSubscriptionStorage(this Configure config, IDocumentStore store, string endpoint)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (store == null) throw new ArgumentNullException("store");

            config.Configurer.ConfigureComponent<RavenSubscriptionStorage>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Store, store)
                .ConfigureProperty(x => x.Endpoint, endpoint);

            return config;
        }
    }
}