namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Transactions;
    using Settings;

    /// <summary>
    /// Settings relates to transactions
    /// </summary>
    public class TransactionSettings
    {
        internal TransactionSettings(ReadOnlySettings settings)
        {
            IsTransactional = settings.Get<bool>("Transactions.Enabled");
            TransactionTimeout = settings.Get<TimeSpan>("Transactions.DefaultTimeout");
            IsolationLevel = settings.Get<IsolationLevel>("Transactions.IsolationLevel");
            SuppressDistributedTransactions = settings.Get<bool>("Transactions.SuppressDistributedTransactions");
            DoNotWrapHandlersExecutionInATransactionScope = settings.Get<bool>("Transactions.DoNotWrapHandlersExecutionInATransactionScope");
        }

        /// <summary>
        /// Create a new settings
        /// </summary>
        /// <param name="isTransactional">Is transactions on</param>
        /// <param name="transactionTimeout">The tx timeout</param>
        /// <param name="isolationLevel">The isolation level</param>
        /// <param name="suppressDistributedTransactions">Should DTC be suppressed</param>
        /// <param name="doNotWrapHandlersExecutionInATransactionScope">Should handlers be wrapped</param>
        public TransactionSettings(bool isTransactional, TimeSpan transactionTimeout, IsolationLevel isolationLevel, bool suppressDistributedTransactions, bool doNotWrapHandlersExecutionInATransactionScope)
        {
            IsTransactional = isTransactional;
            TransactionTimeout = transactionTimeout;
            IsolationLevel = isolationLevel;
            SuppressDistributedTransactions = suppressDistributedTransactions;
            DoNotWrapHandlersExecutionInATransactionScope = doNotWrapHandlersExecutionInATransactionScope;
        }

     
        /// <summary>
        /// Sets whether or not the transport is transactional.
        /// </summary>
        public bool IsTransactional { get; set; }

        /// <summary>
        /// Property for getting/setting the period of time when the transaction times out.
        /// Only relevant when <see cref="IsTransactional"/> is set to true.
        /// </summary>
        public TimeSpan TransactionTimeout { get; set; }

        /// <summary>
        /// Property for getting/setting the isolation level of the transaction scope.
        /// Only relevant when <see cref="IsTransactional"/> is set to true.
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// If true the transport won't enlist in distributed transactions
        /// </summary>
        public bool SuppressDistributedTransactions { get; set; }

        /// <summary>
        /// Controls if the message handlers should be wrapped in a <see cref="TransactionScope"/>
        /// </summary>
        public bool DoNotWrapHandlersExecutionInATransactionScope { get; set; }
    }
}