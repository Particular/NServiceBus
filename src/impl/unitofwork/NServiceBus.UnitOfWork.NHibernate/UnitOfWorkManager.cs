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

            if (NoAmbientTransaction())
            {
                session.BeginTransaction();
            }
        }

        void IManageUnitsOfWork.End()
        {
            if (SessionFactory == null) return;

            if (!NoAmbientTransaction()) return;

            var session = SessionFactory.GetCurrentSession();

            session.Transaction.Commit();
            session.Transaction.Dispose();

            session.Close();
        }

        void IManageUnitsOfWork.Error()
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
