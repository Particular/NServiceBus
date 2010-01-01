using System.Transactions;
using NHibernate;
using NHibernate.Context;

namespace NServiceBus.SagaPersisters.NHibernate
{
    /// <summary>
    /// Message module that manages NHibernate sessions.
    /// At the beginning of message handling, a session is opened,
    /// as the end, it is closed.
    /// </summary>
    public class NHibernateMessageModule : IMessageModule
    {
        void IMessageModule.HandleBeginMessage()
        {
            if (SessionFactory == null) return;

            var session = SessionFactory.OpenSession();

            CurrentSessionContext.Bind(session);

            if (NoAmbientTransaction())
            {
                session.BeginTransaction();
            }
        }

        void IMessageModule.HandleEndMessage()
        {
            if (SessionFactory == null) return;

            if (!NoAmbientTransaction()) return;

            var session = SessionFactory.GetCurrentSession();

            session.Transaction.Commit();
            session.Transaction.Dispose();

            session.Close();
        }

        void IMessageModule.HandleError()
        {
            if (SessionFactory == null) return;

            //HandleError always run after the transactionscope so we can't check for ambient trans here
            var session = SessionFactory.GetCurrentSession();

            if (session.Transaction.IsActive)
            {
                session.Transaction.Rollback();
                session.Transaction.Dispose();
            }
            if (session.IsOpen)
                session.Close();

        }

        /// <summary>
        /// Injected NHibernate session factory.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }


        private static bool NoAmbientTransaction()
        {
            return Transaction.Current == null;
        }

    }
}
