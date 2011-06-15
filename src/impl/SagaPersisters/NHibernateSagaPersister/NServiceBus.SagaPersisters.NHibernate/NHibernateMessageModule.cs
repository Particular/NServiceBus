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

            session.BeginTransaction();
        }

        void IMessageModule.HandleEndMessage()
        {
            if (SessionFactory == null) return;

            var session = CurrentSessionContext.Unbind(SessionFactory);

            try
            {
              try
              {
                session.Transaction.Commit();
              }
              finally
              {
                session.Transaction.Dispose();
              }
            }
            finally
            {
              session.Dispose();
            }
        }

        void IMessageModule.HandleError()
        {
            if (SessionFactory == null) return;

            if (!CurrentSessionContext.HasBind(SessionFactory))
              return;

            var session = CurrentSessionContext.Unbind(SessionFactory);

            try
            {
              try
              {
                if (session.Transaction.IsActive)
                  session.Transaction.Rollback();
              }
              finally
              {
                session.Transaction.Dispose();
              }
            }
            finally
            {
              session.Dispose();
            }
        }

        /// <summary>
        /// Injected NHibernate session factory.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }
    }
}
