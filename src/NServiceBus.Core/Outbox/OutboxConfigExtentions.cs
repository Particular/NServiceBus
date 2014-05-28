namespace NServiceBus
{
    /// <summary>
    /// Config methods for the outbox
    /// </summary>
    public static class OutboxConfigExtentions
    {
        /// <summary>
        /// Eanbles outbox operations
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure EnableOutbox(this Configure config)
        {

            config.Transactions.Advanced(t =>
            {
                t.DisableDistributedTransactions();
                t.DoNotWrapHandlersExecutionInATransactionScope();
            });

            config.Features(f=>f.Enable<Features.Outbox>());

            return config;
        }
    }
}