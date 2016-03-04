namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Settings that applies to the transport.
    /// </summary>
    public class TransportConfig : ConfigurationSection
    {
        /// <summary>
        /// Specifies the maximum concurrency level this endpoint is able to support.
        /// </summary>
        [ConfigurationProperty("MaximumConcurrencyLevel", IsRequired = false, DefaultValue = 0)]
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "EndpointConfiguration.LimitMessageProcessingConcurrencyTo")]
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
        /// The max throughput for the transport. This allows the user to throttle their endpoint if needed.
        /// </summary>
        [ConfigurationProperty("MaximumMessageThroughputPerSecond", IsRequired = false, DefaultValue = -1)]
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6", 
            RemoveInVersion = "7", 
            Message = "Message throughput throttling has been removed. Consult the documentation for further information.")]
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