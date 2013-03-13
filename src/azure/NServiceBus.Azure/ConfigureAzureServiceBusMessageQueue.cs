using System;
using System.Configuration;
using System.Transactions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.Unicast.Queuing.Azure.ServiceBus;

namespace NServiceBus
{
    using Transports;

    public static class ConfigureAzureServiceBusMessageQueue
    {
        public static Configure AzureServiceBusMessageQueue(this Configure config)
        {
            var configSection = Configure.GetConfigSection<AzureServiceBusQueueConfig>();

            if (configSection == null)
                throw new ConfigurationErrorsException("No AzureServiceBusQueueConfig configuration section found");

            Address.InitializeAddressMode(AddressMode.Remote);

            ServiceBusEnvironment.SystemConnectivity.Mode = (ConnectivityMode) Enum.Parse(typeof(ConnectivityMode), configSection.ConnectivityMode);

            if(string.IsNullOrEmpty(configSection.ConnectionString) && (string.IsNullOrEmpty(configSection.IssuerKey) || string.IsNullOrEmpty(configSection.ServiceNamespace) ))
            {
                throw new ConfigurationErrorsException("No Servicebus Connection information specified, either set the ConnectionString or set the IssuerKey and ServiceNamespace properties");
            }
        
            NamespaceManager namespaceClient;
            MessagingFactory factory;
            Uri serviceUri;
            if(!string.IsNullOrEmpty(configSection.ConnectionString))
            {
                namespaceClient = NamespaceManager.CreateFromConnectionString(configSection.ConnectionString);
                serviceUri = namespaceClient.Address;
                factory = MessagingFactory.CreateFromConnectionString(configSection.ConnectionString);
            }
            else
            {
                var credentials = TokenProvider.CreateSharedSecretTokenProvider(configSection.IssuerName, configSection.IssuerKey);
                serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", configSection.ServiceNamespace, string.Empty);
                namespaceClient = new NamespaceManager(serviceUri, credentials);
                factory = MessagingFactory.Create(serviceUri, credentials);
            }
            Address.OverrideDefaultMachine(serviceUri.ToString());

            config.Configurer.RegisterSingleton<NamespaceManager>(namespaceClient); 
            config.Configurer.RegisterSingleton<MessagingFactory>(factory);

            config.Configurer.ConfigureComponent<AzureServiceBusMessageQueueSender>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<AzureServiceBusDequeueStrategy>(DependencyLifecycle.InstancePerCall);
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

            // make sure the transaction stays open a little longer than the long poll.
            Configure.Transactions.Advanced(settings => settings.DefaultTimeout(TimeSpan.FromSeconds(configSection.ServerWaitTime*1.1)).IsolationLevel(IsolationLevel.Serializable));

            if (!string.IsNullOrEmpty(configSection.QueueName))
            {
                Configure.Instance.DefineEndpointName(configSection.QueuePerInstance? QueueIndividualizer.Individualize(configSection.QueueName): configSection.QueueName);
            }
            else if (RoleEnvironment.IsAvailable)
            {
                Configure.Instance.DefineEndpointName(RoleEnvironment.CurrentRoleInstance.Role.Name);
            }
            Address.InitializeLocalAddress(Configure.EndpointName);


            return config;
        }
    }

    public class UseTopicPublishingByDefault: IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            var config = Configure.Instance;
            if (!config.Configurer.HasComponent<IPublishMessages>() && !config.Configurer.HasComponent<IManageSubscriptions>())
            {
                var configSection = Configure.GetConfigSection<AzureServiceBusQueueConfig>();
                
                config.Configurer.ConfigureComponent<AzureServicebusSubscriptionClientCreator>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServiceBusTopicSubscriptionManager>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServiceBusTopicPublisher>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<AzureServiceBusTopicNotifier>(DependencyLifecycle.InstancePerCall);
                
                config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
                config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BatchSize, configSection.BatchSize);
                config.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);
                config.Configurer.ConfigureProperty<AzureServiceBusTopicPublisher>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueSender>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
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