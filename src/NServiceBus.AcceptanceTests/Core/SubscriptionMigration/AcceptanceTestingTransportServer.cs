namespace NServiceBus.AcceptanceTests.Core.SubscriptionMigration;

using System;
using System.Threading.Tasks;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;

class AcceptanceTestingTransportServer : IEndpointSetupTemplate
{
    readonly bool useNativePubSub;

    public AcceptanceTestingTransportServer(bool useNativePubSub)
    {
        this.useNativePubSub = useNativePubSub;
    }

    public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
    {
        var configuration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);
        configuration.EnableInstallers();

        var recoverability = configuration.Recoverability();
        recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
        recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
        configuration.SendFailedMessagesTo("error");

        var transportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(useNativePubSub, true);
        await transportConfiguration.Configure(endpointCustomizationConfiguration.EndpointName, configuration, runDescriptor.Settings, endpointCustomizationConfiguration.PublisherMetadata);
        runDescriptor.OnTestCompleted(_ => transportConfiguration.Cleanup());

        var persistenceConfiguration = new ConfigureEndpointAcceptanceTestingPersistence();
        await persistenceConfiguration.Configure(endpointCustomizationConfiguration.EndpointName, configuration, runDescriptor.Settings, endpointCustomizationConfiguration.PublisherMetadata);
        runDescriptor.OnTestCompleted(_ => persistenceConfiguration.Cleanup());

        configuration.UseSerialization<SystemJsonSerializer>();

        await configurationBuilderCustomization(configuration);

        // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
        configuration.ScanTypesForTest(endpointCustomizationConfiguration);

        return configuration;
    }
}