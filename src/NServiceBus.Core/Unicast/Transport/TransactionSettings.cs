namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Transactions;
    using Settings;

    /// <summary>
    /// Settings related to transactions.
    /// </summary>
    public partial class TransactionSettings
    {
        internal TransactionSettings(ReadOnlySettings settings)
        {
            IsTransactional = settings.Get<bool>("Transactions.Enabled");
            TransactionTimeout = settings.Get<TimeSpan>("Transactions.DefaultTimeout");
            IsolationLevel = settings.Get<IsolationLevel>("Transactions.IsolationLevel");
            SuppressDistributedTransactions = settings.Get<bool>("Transactions.SuppressDistributedTransactions");
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
        /// If true the transport won't enlist in distributed transactions.
        /// </summary>
        public bool SuppressDistributedTransactions { get; set; }
    }
}