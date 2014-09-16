namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Threading;

    /// <summary>
    /// Msmq unit of work to be used in non DTC mode.
    /// </summary>
    public class MsmqUnitOfWork : IDisposable
    {
        /// <summary>
        /// Current <see cref="MessageQueueTransaction"/>.
        /// </summary>
        public MessageQueueTransaction Transaction
        {
            get { return currentTransaction.Value; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            //Injected
        }

        internal void SetTransaction(MessageQueueTransaction msmqTransaction)
        {
            currentTransaction.Value = msmqTransaction;
        }

        /// <summary>
        /// Checks whether a <see cref="MessageQueueTransaction"/> exists.
        /// </summary>
        /// <returns><code>true</code> if a <see cref="MessageQueueTransaction"/> is currently in progress, otherwise <code>false</code>.</returns>
        public bool HasActiveTransaction()
        {
            return currentTransaction.IsValueCreated;
        }

        internal void ClearTransaction()
        {
            currentTransaction.Value = null;
        }

        ThreadLocal<MessageQueueTransaction> currentTransaction = new ThreadLocal<MessageQueueTransaction>();
    }
}