using System;
using System.Configuration;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.Config.Conventions;
using NServiceBus.Unicast.Queuing.AppFabric;

namespace NServiceBus
{
    public static class ConfigureAppFabricMessageQueue
    {
        public static Configure AppFabricMessageQueue(this Configure config)
        {
            var configSection = Configure.GetConfigSection<AppFabricQueueConfig>();

            if (configSection == null)
                throw new ConfigurationErrorsException("No AppFabricQueueConfig configuration section found");

            if (configSection.QueuePerInstance)
                Configure.Instance.CustomConfigurationSource(new IndividualQueueConfigurationSource(Configure.ConfigurationSource));
    
            var credentials = TokenProvider.CreateSharedSecretTokenProvider(configSection.IssuerName, configSection.IssuerKey);
            var serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", configSection.ServiceNamespace, string.Empty);
            var namespaceClient = new NamespaceManager(serviceUri, credentials);
            var factory = MessagingFactory.Create(serviceUri, credentials);

            config.Configurer.RegisterSingleton<NamespaceManager>(namespaceClient); 
            config.Configurer.RegisterSingleton<MessagingFactory>(factory);

            config.Configurer.ConfigureComponent<AppFabricMessageQueue>(DependencyLifecycle.SingleInstance);
            
            Configure.Instance.Configurer.ConfigureProperty<AppFabricMessageQueue>(t => t.LockDuration, TimeSpan.FromMilliseconds(configSection.LockDuration));
            Configure.Instance.Configurer.ConfigureProperty<AppFabricMessageQueue>(t => t.MaxSizeInMegabytes, configSection.MaxSizeInMegabytes);
            Configure.Instance.Configurer.ConfigureProperty<AppFabricMessageQueue>(t => t.RequiresDuplicateDetection, configSection.RequiresDuplicateDetection);
            Configure.Instance.Configurer.ConfigureProperty<AppFabricMessageQueue>(t => t.RequiresSession, configSection.RequiresSession);
            Configure.Instance.Configurer.ConfigureProperty<AppFabricMessageQueue>(t => t.DefaultMessageTimeToLive, TimeSpan.FromMilliseconds(configSection.DefaultMessageTimeToLive));
            Configure.Instance.Configurer.ConfigureProperty<AppFabricMessageQueue>(t => t.EnableDeadLetteringOnMessageExpiration, configSection.EnableDeadLetteringOnMessageExpiration);
            Configure.Instance.Configurer.ConfigureProperty<AppFabricMessageQueue>(t => t.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMilliseconds(configSection.DuplicateDetectionHistoryTimeWindow));
            Configure.Instance.Configurer.ConfigureProperty<AppFabricMessageQueue>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);
            Configure.Instance.Configurer.ConfigureProperty<AppFabricMessageQueue>(t => t.EnableBatchedOperations, configSection.EnableBatchedOperations);

            if (!string.IsNullOrEmpty(configSection.QueueName))
            {
                Configure.Instance.DefineEndpointName(configSection.QueueName);
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