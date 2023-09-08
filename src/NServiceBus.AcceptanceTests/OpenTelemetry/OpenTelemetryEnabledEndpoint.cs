namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Threading.Tasks;
using AcceptanceTesting.EndpointTemplates;
using AcceptanceTesting.Support;

public class OpenTelemetryEnabledEndpoint : DefaultServer
{
    public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
        EndpointCustomizationConfiguration endpointConfiguration,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
        base.GetConfiguration(runDescriptor, endpointConfiguration, configuration =>
        {
            configuration.EnableOpenTelemetry();
            return configurationBuilderCustomization(configuration);
        });
}