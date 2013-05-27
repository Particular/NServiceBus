namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.Transactions;
    using Azure;
    using Config;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Serialization;
    using Settings;
    using Transports;
    using Unicast.Queuing.Azure.ServiceBus;

    public class AzureServiceBusTransport : ConfigureTransport<AzureServiceBus>
    {
        protected override void InternalConfigure(Configure config)
        {
            if (IsRoleEnvironmentAvailable() && !IsHostedIn.ChildHostProcess())
            {
                config.AzureConfigurationSource();
            }

            if (!config.Configurer.HasComponent<IMessageSerializer>())
            {
                NServiceBus.Configure.Serialization.Json();
            }

            var configSection = NServiceBus.Configure.GetConfigSection<AzureServiceBusQueueConfig>();

            if (configSection != null && !string.IsNullOrEmpty(configSection.QueueName))
            {
                NServiceBus.Configure.Instance.DefineEndpointName(configSection.QueuePerInstance ? QueueIndividualizer.Individualize(configSection.QueueName) : configSection.QueueName);
                Address.InitializeLocalAddress(NServiceBus.Configure.EndpointName);
            }
            else if (IsRoleEnvironmentAvailable())
            {
                NServiceBus.Configure.Instance.DefineEndpointName(RoleEnvironment.CurrentRoleInstance.Role.Name);
                Address.InitializeLocalAddress(NServiceBus.Configure.EndpointName);
            }

            var serverWaitTime = AzureServicebusDefaults.DefaultServerWaitTime;

            if (configSection != null)
                serverWaitTime = configSection.ServerWaitTime;

            // make sure the transaction stays open a little longer than the long poll.
            NServiceBus.Configure.Transactions.Advanced(settings => settings.DefaultTimeout(TimeSpan.FromSeconds(serverWaitTime * 1.1)).IsolationLevel(IsolationLevel.Serializable));

            
            Enable<AzureServiceBusTransport>();
            EnableByDefault<TimeoutManager>();
            AzureServiceBusPersistence.UseAsDefault();
        }

     

        static bool IsRoleEnvironmentAvailable()
        {
            try
            {
                return RoleEnvironment.IsAvailable;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void Initialize()
        {
            var configSection = NServiceBus.Configure.GetConfigSection<AzureServiceBusQueueConfig>();

            ServiceBusEnvironment.SystemConnectivity.Mode =  configSection == null ?  ConnectivityMode.Tcp : (ConnectivityMode)Enum.Parse(typeof(ConnectivityMode), configSection.ConnectivityMode);

            var connectionString = SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");

            if (string.IsNullOrEmpty(connectionString) && configSection != null)
                connectionString = configSection.ConnectionString;

            if (string.IsNullOrEmpty(connectionString) && (configSection == null || string.IsNullOrEmpty(configSection.IssuerKey) || string.IsNullOrEmpty(configSection.ServiceNamespace)))
            {
                throw new ConfigurationErrorsException("No Servicebus Connection information specified, either set the ConnectionString or set the IssuerKey and ServiceNamespace properties");
            }

            NamespaceManager namespaceClient;
            MessagingFactory factory;
            Uri serviceUri;
            if (!string.IsNullOrEmpty(connectionString))
            {
                namespaceClient = NamespaceManager.CreateFromConnectionString(connectionString);
                serviceUri = namespaceClient.Address;
                factory = MessagingFactory.CreateFromConnectionString(connectionString);
            }
            else
            {
                var credentials = TokenProvider.CreateSharedSecretTokenProvider(configSection.IssuerName, configSection.IssuerKey);
                serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", configSection.ServiceNamespace, string.Empty);
                namespaceClient = new NamespaceManager(serviceUri, credentials);
                factory = MessagingFactory.Create(serviceUri, credentials);
            }
            Address.OverrideDefaultMachine(serviceUri.ToString());

            
            NServiceBus.Configure.Instance.Configurer.RegisterSingleton<NamespaceManager>(namespaceClient);
            NServiceBus.Configure.Instance.Configurer.RegisterSingleton<MessagingFactory>(factory);
            NServiceBus.Configure.Component<AzureServiceBusQueueCreator>(DependencyLifecycle.InstancePerCall);

            var config = NServiceBus.Configure.Instance;
          
            config.Configurer.ConfigureComponent<AzureServiceBusDequeueStrategy>(DependencyLifecycle.InstancePerCall);

            if (configSection == null)
            {
                //hack: just to get the defaults, we should refactor this to support specifying the values on the NServiceBus/Transport connection string as well
                configSection = new AzureServiceBusQueueConfig(); 
            }

            if (!config.Configurer.HasComponent<ISendMessages>())
            {
                config.Configurer.ConfigureComponent<AzureServiceBusMessageQueueSender>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureProperty<AzureServiceBusMessageQueueSender>(t => t.MaxDeliveryCount, configSection.MaxDeliveryCount);

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

            if (!config.Configurer.HasComponent<IPublishMessages>() &&
                !config.Configurer.HasComponent<IManageSubscriptions>())
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

        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "todo - refactor the transport to use a connection string instead of a custom section"; }
        }
    }
}