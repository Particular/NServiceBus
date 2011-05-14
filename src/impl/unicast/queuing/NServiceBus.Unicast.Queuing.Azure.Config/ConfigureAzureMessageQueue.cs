using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Queuing.Azure;

namespace NServiceBus
{
    public static class ConfigureAzureMessageQueue
    {
        public static Configure AzureMessageQueue(this Configure config)
        {
            CloudQueueClient queueClient;

            var configSection = Configure.GetConfigSection<AzureQueueConfig>();

            if (configSection != null)
            {
                queueClient = CloudStorageAccount.Parse(configSection.ConnectionString)
                                        .CreateCloudQueueClient();
            }
            else
            {
                queueClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();
            }

            config.Configurer.RegisterSingleton<CloudQueueClient>(queueClient);
       
            config.Configurer.ConfigureComponent<AzureMessageQueue>(DependencyLifecycle.SingleInstance);

            if (configSection != null)
            {
                Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueue>(t => t.PurgeOnStartup, configSection.PurgeOnStartup);
                Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueue>(t => t.MaximumWaitTimeWhenIdle, configSection.MaximumWaitTimeWhenIdle);
                Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueue>(t => t.MessageInvisibleTime, configSection.MessageInvisibleTime);
                Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueue>(t => t.PeekInterval, configSection.PeekInterval);
            }

            return config;
        }

        /// <summary>
        /// Requests that the incoming queue be purged of all messages when the bus is started.
        /// All messages in this queue will be deleted if this is true.
        /// Setting this to true may make sense for certain smart-client applications, 
        /// but rarely for server applications.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure PurgeQueueOnStartup(this Configure config, bool value)
        {
            Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueue>(t => t.PurgeOnStartup, value);

            return config;
        }

        /// <summary>
        /// Sets the amount of time, in milliseconds, to add to the time to wait before checking for a new message
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure PeekInterval(this Configure config, int value)
        {
            Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueue>(t => t.PeekInterval, value);

            return config;
        }

        /// <summary>
        /// Sets the maximum amount of time, in milliseconds, that the queue will wait before checking for a new message
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure MaximumWaitTimeWhenIdle(this Configure config, int value)
        {
            Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueue>(t => t.MaximumWaitTimeWhenIdle, value);

            return config;
        }

        /// <summary>
        /// Controls how long messages should be invisible to other callers when receiving messages from the queue
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure MessageInvisibleTime(this Configure config, int value)
        {
            Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueue>(t => t.MessageInvisibleTime, value);

            return config;
        }

        /// <summary>
        /// Controls how many messages should be read from the queue at once
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure BatchSize(this Configure config, int value)
        {
            Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueue>(t => t.BatchSize, value);

            return config;
        }
    }
}