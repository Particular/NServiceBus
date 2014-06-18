namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Threading;

    class MsmqUnitOfWork : IDisposable
    {
        public MessageQueueTransaction Transaction
        {
            get { return currentTransaction.Value; }
        }

        public void Dispose()
        {
            //Injected
        }

        public void SetTransaction(MessageQueueTransaction msmqTransaction)
        {
            currentTransaction.Value = msmqTransaction;
        }

        public bool HasActiveTransaction()
        {
            return currentTransaction.IsValueCreated;
        }

        public void ClearTransaction()
        {
            currentTransaction.Value = null;
        }

        ThreadLocal<MessageQueueTransaction> currentTransaction = new ThreadLocal<MessageQueueTransaction>();
    }
}