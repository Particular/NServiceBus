namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes
{
    using Apache.NMS;
    using SessionFactories;

    public class ActiveMqTransaction : ITransactionScope
    {
        ISessionFactory sessionFactory;
        ISession session;
        bool doRollback = true;

        public ActiveMqTransaction(ISessionFactory sessionFactory, ISession session)
        {
            this.sessionFactory = sessionFactory;
            this.session = session;

            this.sessionFactory.SetSessionForCurrentThread(this.session);
        }

        public void MessageAccepted(IMessage message)
        {
        }

        public void Complete()
        {
            session.Commit();
            doRollback = false;
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            if (doRollback)
            {
                session.Rollback();
            }

            sessionFactory.RemoveSessionForCurrentThread();
        }
    }
}