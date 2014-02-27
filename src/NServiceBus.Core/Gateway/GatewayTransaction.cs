namespace NServiceBus.Gateway
{
    using System;
    using System.Transactions;
    using Config;

    internal static class GatewayTransaction
    {
        private static readonly GatewayConfig Config;

        static GatewayTransaction()
        {
            Config = Configure.GetConfigSection<GatewayConfig>();
        }

        internal static TimeSpan Timeout(TimeSpan defaultTimeout)
        {
            if (Config != null && Config.TransactionTimeout > defaultTimeout)
            {
                return Config.TransactionTimeout;
            }

            return defaultTimeout;
        }

        internal static TransactionScope Scope()
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
