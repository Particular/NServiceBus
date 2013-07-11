namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes
{
    using System;

    using Apache.NMS;

    public class NoTransactionScope : ITransactionScope
    {
        bool disposed;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources.
                
            }

            this.disposed = true;
        }

        ~NoTransactionScope()
        {
            this.Dispose(false);
        }

        public void MessageAccepted(IMessage message)
        {
            message.Acknowledge();
        }

        public void Complete()
        {
        }
    }
}