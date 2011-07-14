using System.Configuration;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Description;
using Microsoft.ServiceBus.Messaging;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Queuing.AppFabric;

namespace NServiceBus
{
    public static class ConfigureAppFabricQueue
    {
        public static Configure AppFabricMessageQueue(this Configure config)
        {
            var configSection = Configure.GetConfigSection<AppFabricQueueConfig>();

            if (configSection == null)
                throw new ConfigurationErrorsException("No AppFabricQueueConfig configuration section found");
            
            var credentials = TransportClientCredentialBase.CreateSharedSecretCredential(configSection.IssuerName, configSection.IssuerKey);
            var managementUri = ServiceBusEnvironment.CreateServiceUri("https", configSection.ServiceNamespace, string.Empty);
            var runtimeUri = ServiceBusEnvironment.CreateServiceUri("sb", configSection.ServiceNamespace, string.Empty);
            var namespaceClient = new ServiceBusNamespaceClient(managementUri, credentials);
            var factory = MessagingFactory.Create(runtimeUri, credentials);

            config.Configurer.RegisterSingleton<ServiceBusNamespaceClient>(namespaceClient); 
            config.Configurer.RegisterSingleton<MessagingFactory>(factory);

            config.Configurer.ConfigureComponent<AppFabricQueue>(DependencyLifecycle.SingleInstance);


            return config;
        }
    }
}