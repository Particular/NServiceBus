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

            Address.InitializeAddressMode(AddressMode.Remote);
            
            if (configSection != null)
            {
                queueClient = CloudStorageAccount.Parse(configSection.ConnectionString).CreateCloudQueueClient();
                Address.OverrideDefaultMachine(configSection.ConnectionString);

                if (configSection.QueuePerInstance)
                    Configure.Instance.CustomConfigurationSource(new IndividualQueueConfigurationSource(Configure.ConfigurationSource));
            }
            else
            {
                queueClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();
                Address.OverrideDefaultMachine(NServiceBus.Unicast.Queuing.Azure.AzureMessageQueue.DefaultConnectionString);
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

            var unicastConfigSection = Configure.GetConfigSection<UnicastBusConfig>();
            var address = unicastConfigSection.LocalAddress;

            if (address == null)
            {
                var msmqConfigSection = Configure.GetConfigSection<MsmqTransportConfig>();
                address = msmqConfigSection.InputQueue;
            }


            Address.InitializeLocalAddress(address);

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

        /// <summary>
        /// Configures a queue per instance
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure QueuePerInstance(this Configure config)
        {
            if(! (Configure.ConfigurationSource is IndividualQueueConfigurationSource))
                Configure.Instance.CustomConfigurationSource(new IndividualQueueConfigurationSource(Configure.ConfigurationSource));

            return config;
        }
    }
}