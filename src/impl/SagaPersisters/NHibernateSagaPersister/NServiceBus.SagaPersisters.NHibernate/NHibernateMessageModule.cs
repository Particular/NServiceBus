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

            session.BeginTransaction(GetIsolationLevel());
        }

        void IMessageModule.HandleEndMessage()
        {
            if (SessionFactory == null) return;

            var session = CurrentSessionContext.Unbind(SessionFactory);

            using (session)
            using (session.Transaction)
            {
                if (!session.Transaction.IsActive)
                    return;

                session.Transaction.Commit();
            }
        }

        void IMessageModule.HandleError()
        {
            if (SessionFactory == null) return;

            if (!CurrentSessionContext.HasBind(SessionFactory))
              return;

            var session = CurrentSessionContext.Unbind(SessionFactory);

            using (session)
            using (session.Transaction)
            {
                if (!session.Transaction.IsActive)
                    return;

                session.Transaction.Rollback();
            }
        }

        /// <summary>
        /// Injected NHibernate session factory.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }

        private System.Data.IsolationLevel GetIsolationLevel()
        {
            if (Transaction.Current == null)
                return System.Data.IsolationLevel.Unspecified;

            switch (Transaction.Current.IsolationLevel)
            {
                case IsolationLevel.Chaos:
                    return System.Data.IsolationLevel.Chaos;
                case IsolationLevel.ReadCommitted:
                    return System.Data.IsolationLevel.ReadCommitted;
                case IsolationLevel.ReadUncommitted:
                    return System.Data.IsolationLevel.ReadUncommitted;
                case IsolationLevel.RepeatableRead:
                    return System.Data.IsolationLevel.RepeatableRead;
                case IsolationLevel.Serializable:
                    return System.Data.IsolationLevel.Serializable;
                case IsolationLevel.Snapshot:
                    return System.Data.IsolationLevel.Snapshot;
                case IsolationLevel.Unspecified:
                    return System.Data.IsolationLevel.Unspecified;
                default:
                    return System.Data.IsolationLevel.Unspecified;
            }
        }
    }
}
