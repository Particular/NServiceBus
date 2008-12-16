using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using ObjectBuilder;
using NServiceBus.SagaPersisters.NHibernate;

namespace NServiceBus
{
    public static class ConfigureNHibernateSagaPersister
    {
        public static Configure NHibernateSagaPersister(this Configure config, ISessionFactory sessionFactory)
        {
            config.Configurer.ConfigureComponent<SagaPersister>(ComponentCallModelEnum.Singlecall)
                .SessionFactory = sessionFactory;

            config.Configurer.ConfigureComponent<NHibernateMessageModule>(ComponentCallModelEnum.Singleton)
                .SessionFactory = sessionFactory;

            return config;
        }
    }
}
