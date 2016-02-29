namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Transactions;
    using NServiceBus.Transports.Msmq;

    /// <summary>
    /// Adds extensions methods to <see cref="TransportExtensions{T}"/> for configuration purposes.
    /// </summary>
    public static class MsmqConfigurationExtensions
    {

        /// <summary>
        /// Set a delegate to use for applying the <see cref="Message.Label"/> property when sending a message.
        /// </summary>
        /// <remarks>
        /// This delegate will be used for all valid messages sent via MSMQ.
        /// This includes, not just standard messages, but also Audits, Errors and all control messages. 
        /// In some cases it may be useful to use the <see cref="Headers.ControlMessageHeader"/> key to determine if a message is a control message.
        /// The only exception to this rule is received messages with corrupted headers. These messages will be forwarded to the error queue with no label applied.
        /// </remarks>
        public static TransportExtensions<MsmqTransport> ApplyLabelToMessages(this TransportExtensions<MsmqTransport> transportExtensions, Func<IReadOnlyDictionary<string, string>, string> labelGenerator)
        {
            Guard.AgainstNull(nameof(labelGenerator), labelGenerator);
            transportExtensions.Settings.Set("msmqLabelGenerator", labelGenerator);
            return transportExtensions;
        }

        /// <summary>
        /// Allows to change the transaction isolation level and timeout for the `TransactionScope` used to receive messages.
        /// </summary>
        /// <remarks>
        /// If not specified the default transaction timeout of the machine will be used and the isolation level will be set to `ReadCommited`.
        /// </remarks> 
        public static TransportExtensions<MsmqTransport> TransactionScopeOptions(this TransportExtensions<MsmqTransport> transportExtensions, TimeSpan? timeout = null, IsolationLevel? isolationLevel = null)
        {
            transportExtensions.Settings.Set<MsmqScopeOptions>(new MsmqScopeOptions(timeout, isolationLevel));
            return transportExtensions;
        }

        /// <summary>
        /// Instructs MSMQ transport to use specified subscription store.
        /// </summary>
        /// <typeparam name="T">Type of subscription store.</typeparam>
        public static SubscriptionStoreSettings<T> UseSubscriptionStore<T>(this TransportExtensions<MsmqTransport> transportExtensions)
            where T : SubscriptionStoreDefinition, new()
        {
            if (transportExtensions == null)
            {
                throw new ArgumentNullException(nameof(transportExtensions));
            }
            transportExtensions.Settings.Set<SubscriptionStoreDefinition>(new T());
            var settings = new SubscriptionStoreSettings<T>(transportExtensions.Settings);
            return settings;
        }
    }
}