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

            return RavenPersistence(config, store);
        }

        public static Configure RavenPersistence(this Configure config, string connectionStringName)
        {

            var connectionStringEntry = ConfigurationManager.ConnectionStrings[connectionStringName];

            if(connectionStringEntry == null)
                throw new ConfigurationErrorsException(string.Format("No connection string named {0} was found",connectionStringName));

            string database = null;

            if (!connectionStringEntry.ConnectionString.Contains("DefaultDatabase"))
                database = databaseNamingConvention();
            

            return RavenPersistence(config, connectionStringName, database);
        }

        public static Configure RavenPersistence(this Configure config, string connectionStringName, string database)
        {
            var store = new DocumentStore
            {
                ConnectionStringName = connectionStringName,
                ResourceManagerId = RavenPersistenceConstants.DefaultResourceManagerId,
            };

            if (!string.IsNullOrEmpty(database))
                store.DefaultDatabase = database;

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


        public static Configure DefineRavenDatabaseNamingConvention(this Configure config,Func<string> convention)
        {
            databaseNamingConvention = convention;

            return config;
        }
        static Func<string> databaseNamingConvention = () => Configure.EndpointName;


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