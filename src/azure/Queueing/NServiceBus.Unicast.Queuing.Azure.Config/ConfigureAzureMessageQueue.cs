using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Config.Conventions;
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
            }
            else
            {
                queueClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();
                Address.OverrideDefaultMachine(AzureMessageQueueReceiver.DefaultConnectionString);
            }

            config.Configurer.RegisterSingleton<CloudQueueClient>(queueClient);

            config.Configurer.ConfigureComponent<AzureMessageQueueReceiver>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p=>p.PurgeOnStartup,ConfigurePurging.PurgeRequested);
            config.Configurer.ConfigureComponent<AzureMessageQueueSender>(DependencyLifecycle.InstancePerCall);

            if (configSection != null)
            {
                Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueueReceiver>(t => t.PurgeOnStartup, configSection.PurgeOnStartup);
                Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueueReceiver>(t => t.MaximumWaitTimeWhenIdle, configSection.MaximumWaitTimeWhenIdle);
                Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueueReceiver>(t => t.MessageInvisibleTime, configSection.MessageInvisibleTime);
                Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueueReceiver>(t => t.PeekInterval, configSection.PeekInterval);
            }

            if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
            {
                Configure.Instance.DefineEndpointName(configSection.QueuePerInstance
                                                          ? QueueIndividualizer.Individualize(configSection.QueueName)
                                                          : configSection.QueueName);
            }
            else if (RoleEnvironment.IsAvailable)
            {
                Configure.Instance.DefineEndpointName(RoleEnvironment.CurrentRoleInstance.Role.Name);
            }
            Address.InitializeLocalAddress(Configure.EndpointName);


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
            Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueueReceiver>(t => t.PeekInterval, value);

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
            Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueueReceiver>(t => t.MaximumWaitTimeWhenIdle, value);

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
            Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueueReceiver>(t => t.MessageInvisibleTime, value);

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
            Configure.Instance.Configurer.ConfigureProperty<AzureMessageQueueReceiver>(t => t.BatchSize, value);

            return config;
        }

        /// <summary>
        /// Configures a queue per instance
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure QueuePerInstance(this Configure config)
        {
            Configure.Instance.DefineEndpointName(QueueIndividualizer.Individualize(Configure.EndpointName));
            Address.InitializeLocalAddress(Configure.EndpointName);
            return config;
        }
    }
}