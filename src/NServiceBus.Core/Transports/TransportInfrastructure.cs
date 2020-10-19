using NServiceBus.Settings;

namespace NServiceBus.Transport
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Routing;

    /// <summary>
    /// Transport infrastructure definitions.
    /// </summary>
    public abstract class TransportInfrastructure
    {
        /// <summary>
        /// 
        /// </summary>
        public abstract bool SupportsTTBR { get; }

        /// <summary>
        /// Gets the highest supported transaction mode for the this transport.
        /// </summary>
        public abstract TransportTransactionMode TransactionMode { get; }

        /// <summary>
        /// Gets the factories to receive message.
        /// </summary>
        public abstract Task<IPushMessages> CreateReceiver(ReceiveSettings receiveSettings);

        /// <summary>
        /// 
        /// </summary>
        public IDispatchMessages Dispatcher { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual Task ValidateNServiceBusSettings(ReadOnlySettings settings)
        {
            // this is only called when the transport is hosted as part of NServiceBus. No need to call this as "raw users".
            // pass a settings type that only allows "tryGet".
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class EndpointAddress
    {
        /// <summary>
        /// 
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// A specific discriminator for scale-out purposes.
        /// </summary>
        public string Discriminator { get; }

        /// <summary>
        /// Returns all the differentiating properties of this instance.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Qualifier { get; }

        /// <summary>
        /// 
        /// </summary>
        public EndpointAddress(string endpoint, string discriminator, IReadOnlyDictionary<string, string> properties, string qualifier)
        {
            Endpoint = endpoint;
            Discriminator = discriminator;
            Properties = properties;
            Qualifier = qualifier;
        }

        /// <summary>
        /// 
        /// </summary>
        public LogicalAddress ToLogicalAddress()
        {
            var logicalAddress = LogicalAddress.CreateRemoteAddress(new EndpointInstance(Endpoint, Discriminator, Properties));

            if (Qualifier != null)
            {
                return logicalAddress.CreateQualifiedAddress(Qualifier);
            }

            return logicalAddress;
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

        /// <summary>
        /// 
        /// </summary>
        public PushSettings settings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool UsePublishSubscribe { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool PurgeOnStartup { get; set; }
    }
}