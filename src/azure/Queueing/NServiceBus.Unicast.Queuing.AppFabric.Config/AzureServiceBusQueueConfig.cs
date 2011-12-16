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

        [ConfigurationProperty("IssuerName", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultIssuerName)]
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


        [ConfigurationProperty("LockDuration", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultLockDuration)]
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

        [ConfigurationProperty("MaxSizeInMegabytes", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultMaxSizeInMegabytes)]
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

        [ConfigurationProperty("RequiresDuplicateDetection", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultRequiresDuplicateDetection)]
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

        [ConfigurationProperty("RequiresSession", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultRequiresSession)]
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

        [ConfigurationProperty("DefaultMessageTimeToLive", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultDefaultMessageTimeToLive)]
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

        [ConfigurationProperty("EnableDeadLetteringOnMessageExpiration", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultEnableDeadLetteringOnMessageExpiration)]
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

        [ConfigurationProperty("DuplicateDetectionHistoryTimeWindow", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultDuplicateDetectionHistoryTimeWindow)]
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

        [ConfigurationProperty("MaxDeliveryCount", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultMaxDeliveryCount)]
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

        [ConfigurationProperty("EnableBatchedOperations", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultEnableBatchedOperations)]
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

        [ConfigurationProperty("QueuePerInstance", IsRequired = false, DefaultValue = AzureServiceBusMessageQueue.DefaultQueuePerInstance)]
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