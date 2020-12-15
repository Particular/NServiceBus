using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NServiceBus.Transport
{
    /// <summary>
    /// Defines a transport.
    /// </summary>
    public abstract class TransportDefinition
    {
        /// <summary>
        /// Initializes all the factories and supported features for the transport. This method is called right before all features
        /// are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing
        /// default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot
        /// provide information anymore at this stage).
        /// </summary>
        public abstract Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] SendingAddresses, CancellationToken cancellationToken = default);


        /// <summary>
        /// </summary>
        public abstract string ToTransportAddress(QueueAddress address);

        /// <summary>
        /// </summary>
        public abstract IReadOnlyCollection<TransportTransactionMode> SupportedTransactionModes { get; protected set; }

        /// <summary>
        /// </summary>
        public abstract bool SupportsDelayedDelivery { get; }

        /// <summary>
        /// </summary>
        public abstract bool SupportsPublishSubscribe { get; }
    }
}