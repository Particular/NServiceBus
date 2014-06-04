namespace NServiceBus
{
    using System;
    using Outbox;

    /// <summary>
    /// Config methods for the outbox
    /// </summary>
    public static class OutboxConfigExtensions
    {
        /// <summary>
        /// Enables the outbox feature
        /// </summary>
        public static Configure EnableOutbox(this Configure config,Action<OutboxSettings> customizations = null)
        {
            if (customizations != null)
            {
                customizations(new OutboxSettings(config.Settings));
            }

            return config.Transactions(t => t.Advanced(a => a.DisableDistributedTransactions()))
            .Features(f => f.Enable<Features.Outbox>());
        }
    }
}