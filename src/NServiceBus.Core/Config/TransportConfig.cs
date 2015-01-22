namespace NServiceBus.Config
{
    using System.Configuration;
    using Unicast.Transport;

    /// <summary>
    /// Settings that applies to the transport
    /// </summary>
    public class TransportConfig : ConfigurationSection
    {
        /// <summary>
        /// Specifies the maximum concurrency level this <see cref="TransportReceiver"/> is able to support.
        /// </summary>
        [ConfigurationProperty("MaximumConcurrencyLevel", IsRequired = false, DefaultValue = 0)]
        public int MaximumConcurrencyLevel
        {
            get
            {
                return (int)this["MaximumConcurrencyLevel"];
            }
            set
            {
                this["MaximumConcurrencyLevel"] = value;

            }
        }

        /// <summary>
        /// The maximum number of times to retry processing a message
        /// when it fails before moving it to the error queue.
        /// </summary>
        [ConfigurationProperty("MaxRetries", IsRequired = false, DefaultValue = 5)]
        public int MaxRetries
        {
            get
            {
                return (int)this["MaxRetries"];
            }
            set
            {
                this["MaxRetries"] = value;
            }
        }

        /// <summary>
        /// The max throughput for the transport. This allows the user to throttle their endpoint if needed
        /// </summary>
        [ConfigurationProperty("MaximumMessageThroughputPerSecond", IsRequired = false, DefaultValue = -1)]
        public int MaximumMessageThroughputPerSecond
        {
            get
            {
                return (int)this["MaximumMessageThroughputPerSecond"];
            }
            set
            {
                this["MaximumMessageThroughputPerSecond"] = value;
            }
        }
    }
}