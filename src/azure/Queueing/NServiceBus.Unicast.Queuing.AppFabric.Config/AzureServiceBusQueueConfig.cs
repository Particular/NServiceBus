using System.Configuration;
using NServiceBus.Unicast.Queuing.Azure.ServiceBus;

namespace NServiceBus.Config
{
    public class AzureServiceBusQueueConfig : ConfigurationSection
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

        [ConfigurationProperty("IssuerKey", IsRequired = true)]
        public string IssuerKey
        {
            get
            {
                return (string)this["IssuerKey"];
            }
            set
            {
                this["IssuerKey"] = value;
            }
        }

        [ConfigurationProperty("ServiceNamespace", IsRequired = true)]
        public string ServiceNamespace
        {
            get
            {
                return (string)this["ServiceNamespace"];
            }
            set
            {
                this["ServiceNamespace"] = value;
            }
        }

        [ConfigurationProperty("IssuerName", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultIssuerName)]
        public string IssuerName
        {
            get
            {
                return (string)this["IssuerName"];
            }
            set
            {
                this["IssuerName"] = value;
            }
        }


        [ConfigurationProperty("LockDuration", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultLockDuration)]
        public int LockDuration
        {
            get
            {
                return (int)this["LockDuration"];
            }
            set
            {
                this["LockDuration"] = value;
            }
        }

        [ConfigurationProperty("MaxSizeInMegabytes", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultMaxSizeInMegabytes)]
        public long MaxSizeInMegabytes
        {
             get
             {
                 return (long)this["MaxSizeInMegabytes"];
             }
             set
             {
                 this["MaxSizeInMegabytes"] = value;
             }
        }

        [ConfigurationProperty("RequiresDuplicateDetection", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultRequiresDuplicateDetection)]
        public bool RequiresDuplicateDetection
        {
             get
             {
                 return (bool)this["RequiresDuplicateDetection"];
             }
             set
             {
                 this["RequiresDuplicateDetection"] = value;
             }
         }

        [ConfigurationProperty("RequiresSession", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultRequiresSession)]
        public bool RequiresSession
        {
             get
             {
                 return (bool)this["RequiresSession"];
             }
             set
             {
                 this["RequiresSession"] = value;
             }
        }

        [ConfigurationProperty("DefaultMessageTimeToLive", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultDefaultMessageTimeToLive)]
        public long DefaultMessageTimeToLive
         {
             get
             {
                 return (long)this["DefaultMessageTimeToLive"];
             }
             set
             {
                 this["DefaultMessageTimeToLive"] = value;
             }
         }

        [ConfigurationProperty("EnableDeadLetteringOnMessageExpiration", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultEnableDeadLetteringOnMessageExpiration)]
        public bool EnableDeadLetteringOnMessageExpiration
        {
             get
             {
                 return (bool)this["EnableDeadLetteringOnMessageExpiration"];
             }
             set
             {
                 this["EnableDeadLetteringOnMessageExpiration"] = value;
             }
        }

        [ConfigurationProperty("DuplicateDetectionHistoryTimeWindow", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultDuplicateDetectionHistoryTimeWindow)]
        public int DuplicateDetectionHistoryTimeWindow
        {
             get
             {
                 return (int)this["DuplicateDetectionHistoryTimeWindow"];
             }
             set
             {
                 this["DuplicateDetectionHistoryTimeWindow"] = value;
             }
        }

        [ConfigurationProperty("MaxDeliveryCount", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultMaxDeliveryCount)]
        public int MaxDeliveryCount
        {
             get
             {
                 return (int)this["MaxDeliveryCount"];
             }
             set
             {
                 this["MaxDeliveryCount"] = value;
             }
        }

        [ConfigurationProperty("EnableBatchedOperations", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultEnableBatchedOperations)]
        public bool EnableBatchedOperations
        {
             get
             {
                 return (bool)this["EnableBatchedOperations"];
             }
             set
             {
                 this["EnableBatchedOperations"] = value;
             }
        }

        [ConfigurationProperty("QueuePerInstance", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultQueuePerInstance)]
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

        [ConfigurationProperty("ServerWaitTime", IsRequired = false, DefaultValue = AzureServiceBusMessageQueueReceiver.DefaultServerWaitTime)]
        public int ServerWaitTime
        {
            get
            {
                return (int)this["ServerWaitTime"];
            }
            set
            {
                this["ServerWaitTime"] = value;
            }
        }
   }
}