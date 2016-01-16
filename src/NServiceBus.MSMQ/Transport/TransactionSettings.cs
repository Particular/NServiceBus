namespace NServiceBus
{
    using System;
    using System.Transactions;
    using NServiceBus.Settings;

    class TransactionSettings
    {
        internal TransactionSettings(ReadOnlySettings settings)
        {
            TransactionTimeout = settings.Get<TimeSpan>("Transactions.DefaultTimeout");
            IsolationLevel = settings.Get<IsolationLevel>("Transactions.IsolationLevel");
        }

        public TimeSpan TransactionTimeout { get; set; }

        public IsolationLevel IsolationLevel { get; set; }
    }
}