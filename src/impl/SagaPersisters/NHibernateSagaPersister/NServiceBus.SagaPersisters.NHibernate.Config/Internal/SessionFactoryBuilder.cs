using System;
using System.Collections.Generic;
using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.ByteCode.LinFu;
using NHibernate.Context;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;
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
            var configuration = fluentConfiguration.BuildConfiguration();

            new SchemaUpdate(configuration)
                .Execute(false, true);
        }

        private static void ApplyDefaultsTo(FluentConfiguration fluentConfiguration)
        {
            fluentConfiguration.ExposeConfiguration(
                c =>
                    {
                        c.SetProperty("current_session_context_class",typeof(ThreadStaticSessionContext).AssemblyQualifiedName);

                        //default to LinFu if not specifed by user
                        if (!c.Properties.Keys.Contains(PROXY_FACTORY_KEY))
                            c.SetProperty(PROXY_FACTORY_KEY,typeof(ProxyFactoryFactory).AssemblyQualifiedName);



                    }
                );
        }


       
        private const string PROXY_FACTORY_KEY = "proxyfactory.factory_class";
    }
}