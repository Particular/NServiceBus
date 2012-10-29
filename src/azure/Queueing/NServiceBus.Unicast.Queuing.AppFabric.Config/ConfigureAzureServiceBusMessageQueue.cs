using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
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

                config.Configurer.ConfigureComponent<AzureServiceBusMessageQueueCreator>(DependencyLifecycle.SingleInstance);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.ConnectionString, configSection.ConnectionString);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.NamespaceClient, namespaceClient);

                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.RequiresSession, configSection.RequiresSession);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueCreator>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);

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
            Configure.Instance.Configurer.ConfigureProperty<AzureServiceBusMessageQueueReceiver>(t => t.ServerWaitTime, configSection.ServerWaitTime);

            if (!string.IsNullOrEmpty(configSection.QueueName))
            {
                Configure.Instance.DefineEndpointName(configSection.QueuePerInstance
                                                          ? QueueIndividualizer.Individualize(configSection.QueueName)
                                                          : configSection.QueueName);
            }
            else if (string.IsNullOrEmpty(configSection.ConnectionString) && RoleEnvironment.IsAvailable)
            {
                Configure.Instance.DefineEndpointName(RoleEnvironment.CurrentRoleInstance.Role.Name);
            }
            Address.InitializeLocalAddress(Configure.EndpointName);


            return config;
        }
    }
}