namespace NServiceBus.Config
{
    using System.Configuration;
    using Unicast.Queuing.Azure.ServiceBus;

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

        [ConfigurationProperty("IssuerKey", IsRequired = false)]
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

        [ConfigurationProperty("ServiceNamespace", IsRequired = false)]
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

        [ConfigurationProperty("IssuerName", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultIssuerName)]
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

        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultConnectionString)]
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


        [ConfigurationProperty("LockDuration", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultLockDuration)]
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

        [ConfigurationProperty("MaxSizeInMegabytes", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultMaxSizeInMegabytes)]
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

        [ConfigurationProperty("RequiresDuplicateDetection", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultRequiresDuplicateDetection)]
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

        [ConfigurationProperty("RequiresSession", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultRequiresSession)]
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

        [ConfigurationProperty("DefaultMessageTimeToLive", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultDefaultMessageTimeToLive)]
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

        [ConfigurationProperty("EnableDeadLetteringOnMessageExpiration", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultEnableDeadLetteringOnMessageExpiration)]
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

        [ConfigurationProperty("EnableDeadLetteringOnFilterEvaluationExceptions", IsRequired = false, DefaultValue = AzureServicebusDefaults.EnableDeadLetteringOnFilterEvaluationExceptions)]
        public bool EnableDeadLetteringOnFilterEvaluationExceptions
        {
             get
             {
                 return (bool)this["EnableDeadLetteringOnFilterEvaluationExceptions"];
             }
             set
             {
                 this["EnableDeadLetteringOnFilterEvaluationExceptions"] = value;
             }
        }


        

        [ConfigurationProperty("DuplicateDetectionHistoryTimeWindow", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultDuplicateDetectionHistoryTimeWindow)]
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

        [ConfigurationProperty("MaxDeliveryCount", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultMaxDeliveryCount)]
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

        [ConfigurationProperty("EnableBatchedOperations", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultEnableBatchedOperations)]
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

        [ConfigurationProperty("QueuePerInstance", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultQueuePerInstance)]
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

        [ConfigurationProperty("ServerWaitTime", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultServerWaitTime)]
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

        [ConfigurationProperty("ConnectivityMode", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultConnectivityMode)]
        public string ConnectivityMode
        {
            get
            {
                return (string)this["ConnectivityMode"];
            }
            set
            {
                this["ConnectivityMode"] = value;
            }
        }

        [ConfigurationProperty("BatchSize", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultBatchSize)]
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

        [ConfigurationProperty("BackoffTimeInSeconds", IsRequired = false, DefaultValue = AzureServicebusDefaults.DefaultBackoffTimeInSeconds)]
        public int BackoffTimeInSeconds
        {
            get
            {
                return (int)this["BackoffTimeInSeconds"];
            }
            set
            {
                this["BackoffTimeInSeconds"] = value;
            }
        }
   }
}