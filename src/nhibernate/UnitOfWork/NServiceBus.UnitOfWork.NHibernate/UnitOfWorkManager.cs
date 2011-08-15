using System.Transactions;
using NHibernate;
using NHibernate.Context;

namespace NServiceBus.UnitOfWork.NHibernate
{
    /// <summary>
    /// Implementation of unit of work management with NHibernate
    /// </summary>
    public class UnitOfWorkManager : IManageUnitsOfWork
    {
        void IManageUnitsOfWork.Begin()
        {
            if (SessionFactory == null) return;

            var session = SessionFactory.OpenSession();

            CurrentSessionContext.Bind(session);

            session.BeginTransaction();
        }

        void IManageUnitsOfWork.End()
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

        void IManageUnitsOfWork.Error()
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
