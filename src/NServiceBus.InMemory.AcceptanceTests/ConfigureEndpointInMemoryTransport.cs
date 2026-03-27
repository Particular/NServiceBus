namespace NServiceBus.AcceptanceTests;

using System.Threading.Tasks;
using AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport;
using NUnit.Framework;

public class ConfigureEndpointInMemoryTransport : IConfigureEndpointTestExecution
{
    public Task Cleanup() => Task.CompletedTask;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.EnforcePublisherMetadataRegistration(endpointName, publisherMetadata);
        configuration.LimitMessageProcessingConcurrencyTo(PushRuntimeSettings.Default.MaxConcurrency);

        configuration.UseTransport(new InMemoryTransport());

        return Task.CompletedTask;
    }
}
