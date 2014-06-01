﻿namespace NServiceBus
{
    /// <summary>
    /// Config methods for the outbox
    /// </summary>
    public static class OutboxConfigExtensions
    {
        /// <summary>
        /// Eanbles outbox operations
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
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