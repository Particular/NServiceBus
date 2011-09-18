using System.Configuration;
using NServiceBus.Unicast.Queuing.AppFabric;

namespace NServiceBus.Config
{
    public class AppFabricQueueConfig : ConfigurationSection
    {

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

        [ConfigurationProperty("IssuerName", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultIssuerName)]
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


        [ConfigurationProperty("LockDuration", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultLockDuration)]
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

        [ConfigurationProperty("MaxSizeInMegabytes", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultMaxSizeInMegabytes)]
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

        [ConfigurationProperty("RequiresDuplicateDetection", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultRequiresDuplicateDetection)]
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

        [ConfigurationProperty("RequiresSession", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultRequiresSession)]
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

        [ConfigurationProperty("DefaultMessageTimeToLive", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultDefaultMessageTimeToLive)]
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

        [ConfigurationProperty("EnableDeadLetteringOnMessageExpiration", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultEnableDeadLetteringOnMessageExpiration)]
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

        [ConfigurationProperty("DuplicateDetectionHistoryTimeWindow", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultDuplicateDetectionHistoryTimeWindow)]
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

        [ConfigurationProperty("MaxDeliveryCount", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultMaxDeliveryCount)]
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

        [ConfigurationProperty("EnableBatchedOperations", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultEnableBatchedOperations)]
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

        [ConfigurationProperty("QueuePerInstance", IsRequired = false, DefaultValue = AppFabricMessageQueue.DefaultQueuePerInstance)]
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