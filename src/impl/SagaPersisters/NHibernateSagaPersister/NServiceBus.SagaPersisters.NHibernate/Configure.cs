using NHibernate.Cfg;
using ObjectBuilder;

namespace NServiceBus.SagaPersisters.NHibernate
{
    public class Configure
    {
        public Configure(IBuilder builder)
        {
            builder.ConfigureComponent<SagaPersister>(ComponentCallModelEnum.Singlecall);

            builder.ConfigureComponent<NHibernateMessageModule>(ComponentCallModelEnum.Singleton);
        }
    }
}
