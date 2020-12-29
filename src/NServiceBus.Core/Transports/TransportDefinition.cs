using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NServiceBus.Transport
{
    /// <summary>
    /// Defines a transport.
    /// </summary>
    public abstract class TransportDefinition
    {
        TransportTransactionMode transportTransactionMode;

        /// <summary>
        /// Creates a new transport definition.
        /// </summary>
        protected TransportDefinition(TransportTransactionMode defaultTransactionMode)
        {
            transportTransactionMode = defaultTransactionMode;
        }

        /// <summary>
        /// Initializes all the factories and supported features for the transport. This method is called right before all features
        /// are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing
        /// default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot
        /// provide information anymore at this stage).
        /// </summary>
        public abstract Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default);


        /// <summary>
        /// </summary>
        public abstract string ToTransportAddress(QueueAddress address);

        /// <summary>
        /// </summary>
        public abstract IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes();

        /// <summary>
        /// Defines the selected TransportTransactionMode for this instance.
        /// </summary>
        public virtual TransportTransactionMode TransportTransactionMode
        {
            get => transportTransactionMode;
            set
            {
                if (!GetSupportedTransactionModes().Contains(value))
                {
                    throw new Exception($"Transaction mode {value} is not supported.");
                }
                transportTransactionMode = value;
            }
        }

        /// <summary>
        /// </summary>
        public abstract bool SupportsDelayedDelivery { get; }

        /// <summary>
        /// </summary>
        public abstract bool SupportsPublishSubscribe { get; }

        /// <summary>
        /// </summary>
        public abstract bool SupportsTTBR { get; }
    }
}