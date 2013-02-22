namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Contains the properties representing the MsmqTransport configuration section.
    /// </summary>
    [ObsoleteEx(Message = "'MsmqTransportConfig' section is obsolete. Please update your configuration to use the new 'TransportConfig' section instead.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public class MsmqTransportConfig : ConfigurationSection
    {
        /// <summary>
        /// The queue to receive messages from in the format
        /// "queue@machine".
        /// </summary>
        [ConfigurationProperty("InputQueue", IsRequired = false)]
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
        [ConfigurationProperty("ErrorQueue", IsRequired = false)]
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
        [ConfigurationProperty("NumberOfWorkerThreads", IsRequired = true, DefaultValue = 1)]
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
