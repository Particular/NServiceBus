using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Common.Logging;
using FluentNHibernate.AutoMap;
using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Conventions;
using Configuration=NHibernate.Cfg.Configuration;

namespace NServiceBus.SagaPersisters.NHibernate.Config.Internal
{
    public class SessionFactoryBuilder
    {
        private readonly IEnumerable<Type> typesToScan;

        public SessionFactoryBuilder(IEnumerable<Type> typesToScan)
        {
            this.typesToScan = typesToScan;
        }

        public ISessionFactory Build(IDictionary<string, string> nhibernateProperties, bool updateSchema)
        {
            var model = Create.SagaPersistenceModel(typesToScan);

            var fluentConfiguration = Fluently.Configure(new Configuration().SetProperties(nhibernateProperties))
                                        .Mappings(m => m.AutoMappings.Add(model));

            ApplyDefaultsTo(fluentConfiguration);
            
            if (updateSchema)
            {
                UpdateDatabaseSchemaUsing(fluentConfiguration);
            }

            return fluentConfiguration.BuildSessionFactory();
        }

        private static void UpdateDatabaseSchemaUsing(FluentConfiguration fluentConfiguration)
        {
            logger.Info("Building schema");
            var configuration = fluentConfiguration.BuildConfiguration();

            new SchemaUpdate(configuration)
                .Execute(false, true);
        }

        private static void ApplyDefaultsTo(FluentConfiguration fluentConfiguration)
        {
            fluentConfiguration.ExposeConfiguration(
                c =>
                    {
                        c.SetProperty("current_session_context_class",
                                      "NHibernate.Context.ThreadStaticSessionContext, NHibernate");

                        //default to LinFu if not specifed by user
                        if (!c.Properties.Keys.Contains(PROXY_FACTORY_KEY))
                            c.SetProperty(PROXY_FACTORY_KEY,
                                          LINFU_PROXYFACTORY);



                    }
                );
        }


       
        public const string PROXY_FACTORY_KEY = "proxyfactory.factory_class";
        public const string LINFU_PROXYFACTORY = "NHibernate.ByteCode.LinFu.ProxyFactoryFactory, NHibernate.ByteCode.LinFu";

        private static readonly ILog logger = LogManager.GetLogger(typeof(SagaPersister));
    }
}