namespace NServiceBus.AcceptanceTests.Core.SubscriptionMigration;

using System;
using System.Threading.Tasks;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;

class AcceptanceTestingTransportServer(bool useNativePubSub) : IEndpointSetupTemplate
{
    public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);
        endpointConfiguration.EnableInstallers();

        var recoverability = endpointConfiguration.Recoverability();
        recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
        recoverability.Immediate(immediate => immediate.NumberOfRetries(0));

        var transportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(useNativePubSub, true);
        await transportConfiguration.Configure(endpointCustomizationConfiguration.EndpointName, endpointConfiguration, runDescriptor.Settings, endpointCustomizationConfiguration.PublisherMetadata);
        runDescriptor.OnTestCompleted(_ => transportConfiguration.Cleanup());

        var persistenceConfiguration = new ConfigureEndpointAcceptanceTestingPersistence();
        await persistenceConfiguration.Configure(endpointCustomizationConfiguration.EndpointName, endpointConfiguration, runDescriptor.Settings, endpointCustomizationConfiguration.PublisherMetadata);
        runDescriptor.OnTestCompleted(_ => persistenceConfiguration.Cleanup());

        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        await configurationBuilderCustomization(endpointConfiguration);

        endpointConfiguration.ScanTypesForTest(endpointCustomizationConfiguration);

        return endpointConfiguration;
    }
}