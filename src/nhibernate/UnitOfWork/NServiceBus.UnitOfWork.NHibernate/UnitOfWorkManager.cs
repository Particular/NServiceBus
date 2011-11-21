using System.Transactions;
using NHibernate;
using NHibernate.Context;

namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;

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

        void IManageUnitsOfWork.End(Exception ex)
        {
            if (SessionFactory == null) return;

            var session = CurrentSessionContext.Unbind(SessionFactory);

            using (session)
            using (session.Transaction)
            {
                if (!session.Transaction.IsActive)
                    return;

                if (ex != null)
                    session.Transaction.Rollback();
                else
                    session.Transaction.Commit();
            }
        }

        /// <summary>
        /// Injected NHibernate session factory.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }
    }
}
