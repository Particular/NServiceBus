namespace NServiceBus.Unicast.Transport.Transactional
{
    using System;
    using System.Transactions;
    using Config;

    public class TransactionSettings
    {
        public TransactionSettings()
        {
            MaxRetries = 5;
            IsTransactional = !Endpoint.IsVolatile;
            TransactionTimeout = TransactionManager.DefaultTimeout;
            IsolationLevel = IsolationLevel.ReadCommitted;
            SuppressDTC = Endpoint.DontUseDistributedTransactions;
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
        /// Sets the maximum number of times a message will be retried
        /// when an exception is thrown as a result of handling the message.
        /// This value is only relevant when <see cref="IsTransactional"/> is true.
        /// </summary>
        /// <remarks>
        /// Default value is 5.
        /// </remarks>
        public int MaxRetries { get; set; }


        /// <summary>
        /// If set to true the transaction scope will be suppressed to avoid the use of DTC
        /// </summary>
        public bool SuppressDTC { get; set; }
    }
}