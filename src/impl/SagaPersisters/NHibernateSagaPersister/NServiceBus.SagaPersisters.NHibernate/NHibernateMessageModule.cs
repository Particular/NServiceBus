using NHibernate;
using NHibernate.Context;

namespace NServiceBus.SagaPersisters.NHibernate
{
    public class NHibernateMessageModule : IMessageModule
    {
        public void HandleBeginMessage()
        {
            ThreadStaticSessionContext.Bind(sessionFactory.OpenSession());
        }

        public void HandleEndMessage()
        {
            ISession session = SessionFactory.GetCurrentSession();

            if (session == null)
                return;

            session.Flush();
            session.Close();
        }

        private ISessionFactory sessionFactory;

        public virtual ISessionFactory SessionFactory
        {
            get { return sessionFactory; }
            set { sessionFactory = value; }
        }
    }
}
