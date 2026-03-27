using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport;

namespace NServiceBus.AcceptanceTests;

public class ConfigureEndpointInMemoryTransport : IConfigureEndpointTestExecution
{
    public Task Cleanup() => Task.CompletedTask;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.EnforcePublisherMetadataRegistration(endpointName, publisherMetadata);
        configuration.LimitMessageProcessingConcurrencyTo(PushRuntimeSettings.Default.MaxConcurrency);

        configuration.UseTransport(new InMemoryTransport(NServiceBusAcceptanceTest.CurrentBroker));

        return Task.CompletedTask;
    }
}