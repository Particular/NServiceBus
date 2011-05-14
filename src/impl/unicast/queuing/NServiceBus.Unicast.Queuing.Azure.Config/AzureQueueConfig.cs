using System.Configuration;

namespace NServiceBus.Config
{
    public class AzureQueueConfig : ConfigurationSection
    {
        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = "UseDevelopmentStorage=true")]
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

        [ConfigurationProperty("PeekInterval", IsRequired = false, DefaultValue = 1000)]
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

        [ConfigurationProperty("MaximumWaitTimeWhenIdle", IsRequired = false, DefaultValue = 60000)]
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

        [ConfigurationProperty("PurgeOnStartup", IsRequired = false, DefaultValue = false)]
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

        [ConfigurationProperty("MessageInvisibleTime", IsRequired = false, DefaultValue = 30000)]
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

        [ConfigurationProperty("BatchSize", IsRequired = false, DefaultValue = 10)]
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
   }
}