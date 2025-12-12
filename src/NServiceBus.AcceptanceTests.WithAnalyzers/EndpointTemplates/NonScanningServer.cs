namespace NServiceBus.AcceptanceTests.EndpointTemplates;

using System;
using System.Threading.Tasks;
using AcceptanceTesting.Support;

public class NonScanningServer : IEndpointSetupTemplate
{
    public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
    {
        var defaultServer = new DefaultServer();
        var endpointConfiguration = await defaultServer.GetConfiguration(runDescriptor, endpointCustomizationConfiguration,
            configurationBuilderCustomization);

        var scanner = endpointConfiguration.AssemblyScanner();
        scanner.Disable = true;

        endpointCustomizationConfiguration.AutoRegisterHandlers = false;
        endpointCustomizationConfiguration.AutoRegisterSagas = false;

        return endpointConfiguration;
    }
}