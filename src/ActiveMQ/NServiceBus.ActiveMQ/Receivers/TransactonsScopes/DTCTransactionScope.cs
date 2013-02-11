namespace NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes
{
    using System;
    using System.Transactions;
    using Apache.NMS;

    public class DTCTransactionScope : ITransactionScope
    {
        private readonly TransactionScope transactionScope;
        private bool complete;
        bool disposed;

        public DTCTransactionScope(ISession session, TransactionOptions transactionOptions)
        {
            transactionScope = new TransactionScope(TransactionScopeOption.Required, transactionOptions);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources.
                transactionScope.Dispose();
                if (!complete)
                {
                    throw new Exception();
                }
            }

            disposed = true;
        }

        ~DTCTransactionScope()
        {
            Dispose(false);
        }

        public void MessageAccepted(IMessage message)
        {
        }

        public void Complete()
        {
            complete = true;
            transactionScope.Complete();
        }
    }
}