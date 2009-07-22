using System;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.ObjectBuilder;
using NServiceBus.SagaPersisters.NHibernate;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;

namespace NServiceBus
{
    public static class Configurer
    {
        public static Configure SQLiteSagaPersister(this Configure config)
        {
            var fileName = ".\\sagas" + Guid.NewGuid().ToString("N") + ".sqllite";
            var persister = SQLiteConfiguration.Standard.UsingFile(fileName);

            var model = Create.SagaPersistenceModel();

            var fluentConfiguration = Fluently.Configure()
                                                .Mappings(m => m.AutoMappings.Add(model))
                                                .Database(persister);

            fluentConfiguration.ExposeConfiguration(
                c =>
                {
                    c.SetProperty("current_session_context_class",
                                  "NHibernate.Context.ThreadStaticSessionContext, NServiceBus.SagaPersisters.SqLite");

                    c.SetProperty("proxyfactory.factory_class",
                                      "NHibernate.ByteCode.LinFu.ProxyFactoryFactory, NServiceBus.SagaPersisters.SqLite");
                }
                );

            var configuration = fluentConfiguration.BuildConfiguration();

            new SchemaUpdate(configuration).Execute(false, true);

            var sessionFactory = fluentConfiguration.BuildSessionFactory();

            config.Configurer.RegisterSingleton<ISessionFactory>(sessionFactory);

            config.Configurer.ConfigureComponent<SagaPersister>(ComponentCallModelEnum.Singlecall);

            config.Configurer.ConfigureComponent<NHibernateMessageModule>(ComponentCallModelEnum.Singleton);

            return config;
        }
    }
}
