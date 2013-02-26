namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactonsScopes
{
    using System;
    using Apache.NMS;

    public class NoTransactionScope : ITransactionScope
    {
        bool disposed;

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
                
            }

            disposed = true;
        }

        ~NoTransactionScope()
        {
            Dispose(false);
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