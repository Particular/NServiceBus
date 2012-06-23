using System;
using System.Configuration;
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

            var credentials = TokenProvider.CreateSharedSecretTokenProvider(configSection.IssuerName, configSection.IssuerKey);
            var serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", configSection.ServiceNamespace, string.Empty);
            var namespaceClient = new NamespaceManager(serviceUri, credentials);
            var factory = MessagingFactory.Create(serviceUri, credentials);

            Address.OverrideDefaultMachine(serviceUri.ToString());

            config.Configurer.RegisterSingleton<NamespaceManager>(namespaceClient); 
            config.Configurer.RegisterSingleton<MessagingFactory>(factory);

            config.Configurer.ConfigureComponent<AzureServiceBusMessageQueueReceiver>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<AzureServiceBusMessageQueueSender>(DependencyLifecycle.InstancePerCall);

            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.RequiresSession, configSection.RequiresSession);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);

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