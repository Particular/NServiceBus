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
        public static Configure EnableOutbox(this Configure config)
        {

            return config.Transactions(t => t.Advanced(a =>
            {
                a.DisableDistributedTransactions();
                a.DoNotWrapHandlersExecutionInATransactionScope();
            }))
            .Features(f => f.Enable<Features.Outbox>());
        }
    }
}