using System;
using NHibernate.Cfg;
using NHibernate;
using System.Threading;

namespace NServiceBus.SagaPersisters.NHibernate
{
    public class NHibernateMessageModule : IMessageModule
    {
        public void HandleBeginMessage()
        {
            Thread.SetData(slot, sessionFactory.OpenSession());
        }

        public void HandleEndMessage()
        {
            ISession session = Thread.GetData(slot) as ISession;

            if (session == null)
                return;

            session.Flush();
            session.Close();
        }

        static NHibernateMessageModule()
        {
            Configuration config = new Configuration();
            config.Configure();

            sessionFactory = config.BuildSessionFactory();

            slot = Thread.AllocateNamedDataSlot(typeof(ISession).Name);
        }

        private static ISessionFactory sessionFactory;
        private static LocalDataStoreSlot slot;
    }
}
