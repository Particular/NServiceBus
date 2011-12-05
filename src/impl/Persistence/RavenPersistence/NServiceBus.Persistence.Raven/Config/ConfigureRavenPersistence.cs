using System;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus
{
    using System.Configuration;
    using Persistence.Raven;
    using Saga;
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
            var connectionStringEntry = ConfigurationManager.ConnectionStrings["RavenDB"];

            //use exsiting config if we can find one
            if (connectionStringEntry != null)
                return RavenPersistence(config,"RavenDB");

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

            store.Conventions.FindTypeTagName = tagNameConvention;

            store.Initialize();

            config.Configurer.RegisterSingleton<IDocumentStore>(store);

            return config;
        }

        public static void DefineRavenTagNameConvention(Func<Type, string> convention)
        {
            tagNameConvention = convention;
        }

        static Func<Type, string> tagNameConvention = t=>
                                                          {
                                                              var tagName = t.Name;
                                                              
                                                              if (typeof(ISagaEntity).IsAssignableFrom(t))
                                                                  tagName= tagName.Replace("Data", "");
                                                              
                                                              return tagName;
                                                          };
    }
}