using NHibernate.Context;
using ObjectBuilder;
using NHibernate;

namespace NServiceBus.SagaPersisters.NHibernate
{
    public class Configure
    {
        public Configure(IBuilder builder, ISessionFactory sessionFactory)
        {
            builder.ConfigureComponent<SagaPersister>(ComponentCallModelEnum.Singlecall)
                .SessionFactory = sessionFactory;

            builder.ConfigureComponent<NHibernateMessageModule>(ComponentCallModelEnum.Singleton)
                .SessionFactory = sessionFactory;
        }
    }
}
