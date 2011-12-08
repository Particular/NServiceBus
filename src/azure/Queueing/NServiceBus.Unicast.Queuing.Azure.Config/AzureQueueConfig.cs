using System.Configuration;
using NServiceBus.Unicast.Queuing.Azure;

namespace NServiceBus.Config
{
    public class AzureQueueConfig : ConfigurationSection
    {
        [ConfigurationProperty("QueueName", IsRequired = false, DefaultValue = null)]
        public string QueueName
        {
            get
            {
                return (string)this["QueueName"];
            }
            set
            {
                this["QueueName"] = value;
            }
        }

        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = AzureMessageQueue.DefaultConnectionString)]
        public string ConnectionString
        {
            get
            {
                return (string)this["ConnectionString"];
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }

        [ConfigurationProperty("PeekInterval", IsRequired = false, DefaultValue = AzureMessageQueue.DefaultPeekInterval)]
        public int PeekInterval
        {
            get
            {
                return (int)this["PeekInterval"];
            }
            set
            {
                this["PeekInterval"] = value;
            }
        }

        [ConfigurationProperty("MaximumWaitTimeWhenIdle", IsRequired = false, DefaultValue = AzureMessageQueue.DefaultMaximumWaitTimeWhenIdle)]
        public int MaximumWaitTimeWhenIdle
        {
            get
            {
                return (int)this["MaximumWaitTimeWhenIdle"];
            }
            set
            {
                this["MaximumWaitTimeWhenIdle"] = value;
            }
        }

        [ConfigurationProperty("PurgeOnStartup", IsRequired = false, DefaultValue = AzureMessageQueue.DefaultPurgeOnStartup)]
        public bool PurgeOnStartup
        {
            get
            {
                return (bool)this["PurgeOnStartup"];
            }
            set
            {
                this["PurgeOnStartup"] = value;
            }
        }

        [ConfigurationProperty("MessageInvisibleTime", IsRequired = false, DefaultValue = AzureMessageQueue.DefaultMessageInvisibleTime)]
        public int MessageInvisibleTime
        {
            get
            {
                return (int)this["MessageInvisibleTime"];
            }
            set
            {
                this["MessageInvisibleTime"] = value;
            }
        }

        [ConfigurationProperty("BatchSize", IsRequired = false, DefaultValue = AzureMessageQueue.DefaultBatchSize)]
        public int BatchSize
        {
            get
            {
                return (int)this["BatchSize"];
            }
            set
            {
                this["BatchSize"] = value;
            }
        }

        [ConfigurationProperty("QueuePerInstance", IsRequired = false, DefaultValue = AzureMessageQueue.DefaultQueuePerInstance)]
        public bool QueuePerInstance
        {
            get
            {
                return (bool)this["QueuePerInstance"];
            }
            set
            {
                this["QueuePerInstance"] = value;
            }
        }
   }
}