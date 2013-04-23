using System;
using System.Configuration;
using System.Transactions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;

namespace NServiceBus
{
    public static class ConfigureAzureServiceBusMessageQueue
    {
        public static bool AzureServiceBusMessageQueueIsUsed = false;

        public static Configure AzureServiceBusMessageQueue(this Configure config)
        {
            AzureServiceBusPersistence.UseAsDefault();

            var configSection = Configure.GetConfigSection<AzureServiceBusQueueConfig>();

            if (configSection == null)
                throw new ConfigurationErrorsException("No AzureServiceBusQueueConfig configuration section found");

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

            AzureServiceBusMessageQueueIsUsed = true;

            return config;
        }
    }
}