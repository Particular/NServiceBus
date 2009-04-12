using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NServiceBus.ObjectBuilder;
using NServiceBus.SagaPersisters.NHibernate;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the NHibernate saga persister.
    /// </summary>
    public static class ConfigureNHibernateSagaPersister
    {
        /// <summary>
        /// Use the NHibernate backed saga persister implementation.
        /// Be aware that this implementation deletes sagas that complete so as not to have the database fill up.
        /// 
        /// Requires that a session factory be configured externally and passed in.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sessionFactory">An externally configured session factory.</param>
        /// <returns></returns>
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
