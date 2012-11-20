namespace NServiceBus.Config
{
    using System.Configuration;

    public class TransportConfig : ConfigurationSection
    {
     
        /// <summary>
        /// The number of worker threads that can process messages in parallel.
        /// </summary>
        [ConfigurationProperty("MaxDegreeOfParallelism", IsRequired = true, DefaultValue = 1)]
        public int MaxDegreeOfParallelism
        {
            get
            {
                return (int)this["MaxDegreeOfParallelism"];
            }
            set
            {
                this["MaxDegreeOfParallelism"] = value;

            }
        }

        /// <summary>
        /// The maximum number of times to retry processing a message
        /// when it fails before moving it to the error queue.
        /// </summary>
        [ConfigurationProperty("MaxRetries", IsRequired = true, DefaultValue = 5)]
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
    }
}