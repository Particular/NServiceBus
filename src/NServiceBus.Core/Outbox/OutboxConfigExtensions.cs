﻿namespace NServiceBus
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
        public static void EnableOutbox(this ConfigurationBuilder config, Action<OutboxSettings> customizations = null)
        {
            if (customizations != null)
            {
                customizations(new OutboxSettings(config.settings));
            }

            config.Transactions(t => t.Advanced(a =>
            {
                a.DisableDistributedTransactions();
                a.DoNotWrapHandlersExecutionInATransactionScope();
            }));
            config.EnableFeature<Features.Outbox>();
        }
    }
}