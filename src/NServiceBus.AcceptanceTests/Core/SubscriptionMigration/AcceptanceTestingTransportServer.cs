namespace NServiceBus.AcceptanceTests.Core.SubscriptionMigration
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using Features;

    class AcceptanceTestingTransportServer : IEndpointSetupTemplate
    {
        readonly bool useNativePubSub;

        public AcceptanceTestingTransportServer(bool useNativePubSub)
        {
            this.useNativePubSub = useNativePubSub;
        }

        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var types = endpointConfiguration.GetTypesScopedByTestClass();

            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.TypesToIncludeInScan(types);
            configuration.EnableInstallers();

            configuration.DisableFeature<TimeoutManager>();

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");

            var transportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(useNativePubSub, true);
            await transportConfiguration.Configure(endpointConfiguration.EndpointName, configuration, runDescriptor.Settings, endpointConfiguration.PublisherMetadata);
            runDescriptor.OnTestCompleted(_ => transportConfiguration.Cleanup());

            configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            var persistenceConfiguration = new ConfigureEndpointAcceptanceTestingPersistence();
            await persistenceConfiguration.Configure(endpointConfiguration.EndpointName, configuration, runDescriptor.Settings, endpointConfiguration.PublisherMetadata);
            runDescriptor.OnTestCompleted(_ => persistenceConfiguration.Cleanup());

            configurationBuilderCustomization(configuration);

            return configuration;
        }
    }
}