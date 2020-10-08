using NServiceBus.Settings;

namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Routing;

    /// <summary>
    /// Transport infrastructure definitions.
    /// </summary>
    public abstract class TransportInfrastructure
    {
        /// <summary>
        /// Returns the list of supported delivery constraints for this transport.
        /// </summary>
        public abstract IEnumerable<Type> DeliveryConstraints { get; }

        /// <summary>
        /// Gets the highest supported transaction mode for the this transport.
        /// </summary>
        public abstract TransportTransactionMode TransactionMode { get; }

        /// <summary>
        /// Gets the factories to receive message.
        /// </summary>
        public abstract TransportReceiveInfrastructure ConfigureReceiveInfrastructure(ReceiveSettings receiveSettings);

        /// <summary>
        /// Gets the factories to send message.
        /// </summary>
        public abstract TransportSendInfrastructure ConfigureSendInfrastructure();

        //// TODO does this need to be standalone or can this be added into receive/subscription infrastructure?
        
        /// <summary>
        /// Gets the factory to manage subscriptions.
        /// </summary>
        public abstract TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure(SubscriptionSettings subscriptionSettings);

        /// <summary>
        /// 
        /// </summary>
        public virtual Task ValidateNServiceBusSettings(ReadOnlySettings settings)
        {
            // this is only called when the transport is hosted as part of NServiceBus. No need to call this as "raw users".
            // pass a settings type that only allows "tryGet".
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns address properties for the local endpoint
        /// </summary>
        public abstract LogicalAddress BuildLocalAddress(string queueName);

        /// <summary>
        /// Converts a given logical address to the transport address.
        /// </summary>
        /// <param name="logicalAddress">The logical address.</param>
        /// <returns>The transport address.</returns>
        public abstract string ToTransportAddress(LogicalAddress logicalAddress);

        /// <summary>
        /// Returns the canonical for of the given transport address so various transport addresses can be effectively compared and
        /// de-duplicated.
        /// </summary>
        /// <param name="transportAddress">A transport address.</param>
        public virtual string MakeCanonicalForm(string transportAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(transportAddress), transportAddress);
            return transportAddress;
        }

        /// <summary>
        /// Performs any action required to warm up the transport infrastructure before starting the endpoint.
        /// </summary>
        public virtual Task Start()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs any action required to cool down the transport infrastructure when the endpoint is stopping.
        /// </summary>
        public virtual Task Stop()
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SubscriptionSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public string LocalAddress { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ReceiveSettings
    {        
        /// <summary>
        /// 
        /// </summary>
        public string ErrorQueueAddress { get; set; } //TODO would be good to know if we're using the default or user provided value

        /// <summary>
        /// 
        /// </summary>
        public string LocalAddress { get; set; }
    }
}