using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.Timeout.Hosting.Windows;
using NServiceBus.Timeout.Hosting.Windows.Persistence;
using NServiceBus.Unicast.Queuing.Azure;
using NServiceBus.Unicast.Queuing.Azure.ServiceBus;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Timeout.Hosting.Azure
{
    public static class ConfigureTimeoutManager
    {
        /// <summary>
        /// Use the in azure timeout persister implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseAzureTimeoutPersister(this Configure config)
        {
            var configSection = Configure.GetConfigSection<AzureTimeoutPersisterConfig>() ?? new AzureTimeoutPersisterConfig();

            config.Configurer.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.InstancePerCall).ConfigureProperty(tp => tp.ConnectionString, configSection.ConnectionString);
            return config;
        }

        /// <summary>
        /// Listen on azure storage.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure ListenOnAzureStorageQueues(this Configure config)
        {
            TimeoutMessageProcessor.MessageReceiverFactory = () =>{
                var queue = config.Builder.Build<AzureMessageQueueReceiver>();
                return new AzureMessageQueueReceiver
                           {
                               Client = queue.Client,
                               PeekInterval = queue.PeekInterval,
                               MaximumWaitTimeWhenIdle = queue.MaximumWaitTimeWhenIdle,
                               PurgeOnStartup = queue.PurgeOnStartup,
                               MessageInvisibleTime = queue.MessageInvisibleTime,
                               BatchSize = queue.BatchSize,
                               MessageSerializer = queue.MessageSerializer
                           };
            };
            return config;
        }

        /// <summary>
        /// Listen on azure servicebus.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure ListenOnAzureServiceBusQueues(this Configure config)
        {
            TimeoutMessageProcessor.MessageReceiverFactory = () =>{
                var queue = config.Builder.Build<AzureServiceBusMessageQueueReceiver>();
                return new AzureServiceBusMessageQueueReceiver{
                    LockDuration = queue.LockDuration,
                    MaxSizeInMegabytes =queue.MaxSizeInMegabytes,
                    RequiresDuplicateDetection = queue.RequiresDuplicateDetection,
                    RequiresSession = queue.RequiresSession,
                    DefaultMessageTimeToLive = queue.DefaultMessageTimeToLive,
                    EnableDeadLetteringOnMessageExpiration = queue.EnableDeadLetteringOnMessageExpiration,
                    DuplicateDetectionHistoryTimeWindow = queue.DuplicateDetectionHistoryTimeWindow,
                    MaxDeliveryCount = queue.MaxDeliveryCount,
                    EnableBatchedOperations = queue.EnableBatchedOperations,
                    Factory = queue.Factory,
                    NamespaceClient = queue.NamespaceClient
                };
            };
            return config;
        }
    }
}