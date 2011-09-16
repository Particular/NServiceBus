using System.Configuration;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Queuing.AppFabric;

namespace NServiceBus
{
    public static class ConfigureAppFabricQueue
    {
        public static Configure AppFabricQueue(this Configure config)
        {
            var configSection = Configure.GetConfigSection<AppFabricQueueConfig>();

            if (configSection == null)
                throw new ConfigurationErrorsException("No AppFabricQueueConfig configuration section found");
    
            var credentials = TokenProvider.CreateSharedSecretTokenProvider(configSection.IssuerName, configSection.IssuerKey);
            var serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", configSection.ServiceNamespace, string.Empty);
            var namespaceClient = new NamespaceManager(serviceUri, credentials);
            var factory = MessagingFactory.Create(serviceUri, credentials);

            config.Configurer.RegisterSingleton<NamespaceManager>(namespaceClient); 
            config.Configurer.RegisterSingleton<MessagingFactory>(factory);

            config.Configurer.ConfigureComponent<AppFabricQueue>(DependencyLifecycle.SingleInstance);


            return config;
        }
    }
}