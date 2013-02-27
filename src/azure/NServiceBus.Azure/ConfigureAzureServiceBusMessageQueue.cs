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
            config.Configurer.ConfigureComponent<AzureServicebusSubscriptionClientCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServiceBusQueueNotifier>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServiceBusTopicSubscriptionManager>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServiceBusTopicPublisher>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServiceBusTopicNotifier>(DependencyLifecycle.InstancePerCall);

            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.RequiresSession, configSection.RequiresSession);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusQueueClientCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BatchSize, configSection.BatchSize);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusQueueNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusTopicPublisher>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueSender>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.RequiresSession, configSection.RequiresSession);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.EnableDeadLetteringOnFilterEvaluationExceptions, configSection.EnableDeadLetteringOnFilterEvaluationExceptions);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            Configure.Instance.Configurer.ConfigureProperty<AzureServicebusSubscriptionClientCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusTopicNotifier>(t => t.ServerWaitTime, configSection.ServerWaitTime);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusTopicNotifier>(t => t.BatchSize, configSection.BatchSize);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusTopicNotifier>(t => t.BackoffTimeInSeconds, configSection.BackoffTimeInSeconds);

            

            // make sure the transaction stays open a little longer than the long poll.
            Configure.Transactions.Advanced(
                settings => settings
                                .DefaultTimeout(TimeSpan.FromSeconds(configSection.ServerWaitTime*1.1))
                                .IsolationLevel(IsolationLevel.Serializable));

            if (!string.IsNullOrEmpty(configSection.QueueName))
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
    }
}