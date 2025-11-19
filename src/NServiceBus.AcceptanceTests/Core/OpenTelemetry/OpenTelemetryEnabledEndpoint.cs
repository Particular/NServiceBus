namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Threading.Tasks;
using AcceptanceTesting.Support;
using EndpointTemplates;

public class OpenTelemetryEnabledEndpoint : DefaultServer
{
    public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
        EndpointCustomizationConfiguration endpointCustomizationConfiguration,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
        base.GetConfiguration(runDescriptor, endpointCustomizationConfiguration, configuration =>
        {
            configuration.EnableOpenTelemetry();
            return configurationBuilderCustomization(configuration);
        });
}