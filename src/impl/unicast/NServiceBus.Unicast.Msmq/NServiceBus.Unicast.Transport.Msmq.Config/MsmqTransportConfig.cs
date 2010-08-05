using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// Contains the properties representing the MsmqTransport configuration section.
    /// </summary>
    public class MsmqTransportConfig : ConfigurationSection
    {
        /// <summary>
        /// The queue to receive messages from in the format
        /// "queue@machine".
        /// </summary>
        [ConfigurationProperty("InputQueue", IsRequired = true)]
        public string InputQueue
        {
            get
            {
                return this["InputQueue"] as string;
            }
            set
            {
                this["InputQueue"] = value;
            }
        }

        /// <summary>
        /// The queue to which to forward messages that could not be processed
        /// in the format "queue@machine".
        /// </summary>
        [ConfigurationProperty("ErrorQueue", IsRequired = true)]
        public string ErrorQueue
        {
            get
            {
                return this["ErrorQueue"] as string;
            }
            set
            {
                this["ErrorQueue"] = value;
            }
        }

        /// <summary>
        /// The number of worker threads that can process messages in parallel.
        /// </summary>
        [ConfigurationProperty("NumberOfWorkerThreads", IsRequired = true)]
        public int NumberOfWorkerThreads
        {
            get
            {
                return (int)this["NumberOfWorkerThreads"];
            }
            set
            {
                this["NumberOfWorkerThreads"] = value;
            }
        }

        /// <summary>
        /// The maximum number of times to retry processing a message
        /// when it fails before moving it to the error queue.
        /// </summary>
        [ConfigurationProperty("MaxRetries", IsRequired = true)]
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
        /// Indicates that queues should not be created.
        /// </summary>
        public static bool DoNotCreateQueues { get; set; }
    }
}
