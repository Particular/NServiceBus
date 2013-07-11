namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;
    using System.Transactions;
    using global::NHibernate;

    /// <summary>
    /// Implementation of unit of work management with NHibernate
    /// </summary>
    public class UnitOfWorkManager : IManageUnitsOfWork
    {
        [ThreadStatic]
        private static ISession currentSession;

        internal ISession GetCurrentSession()
        {
            if (currentSession == null)
            {
                currentSession = SessionFactory.OpenSession();
                currentSession.BeginTransaction(GetIsolationLevel());
            }

            return currentSession;
        }

        void IManageUnitsOfWork.Begin()
        {
            currentSession = null;
        }

        void IManageUnitsOfWork.End(Exception ex)
        {
            if (SessionFactory == null || currentSession == null) return;

            using (currentSession)
            using (currentSession.Transaction)
            {
                if (!currentSession.Transaction.IsActive)
                    return;

                if (ex != null)
                {
                    // Due to a race condition in NH3.3, explicit rollback can cause exceptions and corrupt the connection pool. 
                    // Especially if there are more than one NH session taking part in the DTC transaction
                    //currentSession.Transaction.Rollback();
                }
                else
                    currentSession.Transaction.Commit();
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
