namespace NServiceBus
{
    using Outbox;

    /// <summary>
    /// Config methods for the outbox
    /// </summary>
    public static class OutboxConfigExtensions
    {
        /// <summary>
        /// Enables the outbox feature
        /// </summary>
        public static OutboxSettings EnableOutbox(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            var outboxSettings = new OutboxSettings(config.Settings);
            config.Transactions()
                .DisableDistributedTransactions()
                .DoNotWrapHandlersExecutionInATransactionScope();
            config.EnableFeature<Features.Outbox>();
            return outboxSettings;
        }
    }
}