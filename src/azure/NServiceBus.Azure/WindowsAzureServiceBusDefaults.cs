namespace NServiceBus
{
    using System;
    using Config;
    using Transports;
    using Unicast.Queuing.Azure.ServiceBus;

    public class WindowsAzureServiceBusDefaults: IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            var config = Configure.Instance;
            var configSection = Configure.GetConfigSection<AzureServiceBusQueueConfig>();

            if (ConfigureAzureServiceBusMessageQueue.AzureServiceBusMessageQueueIsUsed && !config.Configurer.HasComponent<IDequeueMessages>())
            {
                config.Configurer.ConfigureComponent<AzureServiceBusDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            }

            if (ConfigureAzureServiceBusMessageQueue.AzureServiceBusMessageQueueIsUsed && !config.Configurer.HasComponent<ISendMessages>())
            {
                config.Configurer.ConfigureComponent<AzureServiceBusMessageQueueSender>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueSender>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);

                if (config.Configurer.HasComponent<AzureServiceBusDequeueStrategy>())
                {
                    config.Configurer.ConfigureComponent<AzureServiceBusQueueNotifier>(DependencyLifecycle.InstancePerCall);
                    config.Configurer.ConfigureComponent<AzureServicebusQueueClientCreator>(DependencyLifecycle.InstancePerCall);
                    config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
                    config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes);
                    config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection);
                    config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.RequiresSession, configSection.RequiresSession);
                    config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
                    config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
                    config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
                    config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                    config.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
                    config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
                    config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BatchSize, configSection.BatchSize);
                    config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);
                }
            }

            if (ConfigureAzureServiceBusMessageQueue.AzureServiceBusMessageQueueIsUsed && 
                !config.Configurer.HasComponent<IPublishMessages>() && 
                !config.Configurer.HasComponent<IManageSubscriptions>()
                && config.Configurer.HasComponent<AzureServiceBusDequeueStrategy>())
            {
                config.Configurer.ConfigureComponent<AzureServicebusSubscriptionClientCreator>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServiceBusTopicSubscriptionManager>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServiceBusTopicPublisher>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServiceBusTopicNotifier>(DependencyLifecycle.InstancePerCall);
                
                config.Configurer.ConfigureProperty<AzureServiceBusTopicPublisher>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.RequiresSession, configSection.RequiresSession);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.EnableDeadLetteringOnFilterEvaluationExceptions, configSection.EnableDeadLetteringOnFilterEvaluationExceptions);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                config.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
                config.Configurer.ConfigureProperty<AzureServiceBusTopicNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
                config.Configurer.ConfigureProperty<AzureServiceBusTopicNotifier>(t => t.BatchSize, configSection.BatchSize);
                config.Configurer.ConfigureProperty<AzureServiceBusTopicNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);
            }
        }
    }
}