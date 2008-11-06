using NHibernate.Cfg;
using ObjectBuilder;

namespace NServiceBus.SagaPersisters.NHibernate
{
    public class Configure
    {
        public Configure(IBuilder builder)
        {
            builder.ConfigureComponent<SagaPersister>(ComponentCallModelEnum.Singlecall);

            Configuration config = new Configuration();
            config.Configure();

            SagaPersister.sessionFactory = config.BuildSessionFactory();
        }
    }
}
