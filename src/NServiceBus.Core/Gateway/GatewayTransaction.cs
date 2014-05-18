namespace NServiceBus.Gateway
{
    using System;
    using System.Transactions;

    class GatewayTransaction
    {
        public TimeSpan Timeout(TimeSpan defaultTimeout)
        {
            if (ConfiguredTimeout.HasValue && ConfiguredTimeout > defaultTimeout)
            {
                return ConfiguredTimeout.Value;
            }

            return defaultTimeout;
        }

        public TimeSpan? ConfiguredTimeout { get; set; }

        public TransactionScope Scope()
        {
            return new TransactionScope(TransactionScopeOption.RequiresNew,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = Timeout(TimeSpan.FromSeconds(30)),
                });
        }
    }
}
