namespace NServiceBus.Config
{
    using System.Configuration;
    using Unicast.Queuing.Azure;

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

        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = AzureMessageQueueReceiver.DefaultConnectionString)]
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

        [ConfigurationProperty("PeekInterval", IsRequired = false, DefaultValue = AzureMessageQueueReceiver.DefaultPeekInterval)]
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

        [ConfigurationProperty("MaximumWaitTimeWhenIdle", IsRequired = false, DefaultValue = AzureMessageQueueReceiver.DefaultMaximumWaitTimeWhenIdle)]
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

        [ConfigurationProperty("PurgeOnStartup", IsRequired = false, DefaultValue = AzureMessageQueueReceiver.DefaultPurgeOnStartup)]
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

        [ConfigurationProperty("MessageInvisibleTime", IsRequired = false, DefaultValue = AzureMessageQueueReceiver.DefaultMessageInvisibleTime)]
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

        [ConfigurationProperty("BatchSize", IsRequired = false, DefaultValue = AzureMessageQueueReceiver.DefaultBatchSize)]
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

        [ConfigurationProperty("QueuePerInstance", IsRequired = false, DefaultValue = AzureMessageQueueReceiver.DefaultQueuePerInstance)]
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