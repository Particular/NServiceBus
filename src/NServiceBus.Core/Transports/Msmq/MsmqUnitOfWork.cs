namespace NServiceBus.Transports.Msmq
{
    using System.Messaging;
    using System.Threading;

    public class MsmqUnitOfWork
    {
        public void SetTransaction(MessageQueueTransaction msmqTransaction)
        {
            currentTransaction.Value = msmqTransaction;
        }

        public bool HasActiveTransaction()
        {
            return currentTransaction.IsValueCreated;
        }

        public MessageQueueTransaction Transaction
        {
            get { return currentTransaction.Value; }
        }
        public void ClearTransaction()
        {
            currentTransaction.Value = null;
        }

        readonly ThreadLocal<MessageQueueTransaction> currentTransaction = new ThreadLocal<MessageQueueTransaction>();
    }
}