namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using Apache.NMS;

    using NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes;

    public class ActiveMqTransaction : ITransactionScope
    {
        private readonly ISessionFactory sessionFactory;
        private readonly ISession session;

        private bool doRollback = true;

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
            this.session.Commit();
            this.doRollback = false;
        }

        public void Dispose()
        {
            if (this.doRollback)
            {
                this.session.Rollback();
            }

            this.sessionFactory.RemoveSessionForCurrentThread();
        }
    }
}