using System.Configuration;

namespace NServiceBus.Config
{
    public class MsmqTransportConfig : ConfigurationSection
    {
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
    }
}
