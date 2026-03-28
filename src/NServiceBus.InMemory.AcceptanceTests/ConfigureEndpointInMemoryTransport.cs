using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NServiceBus.AcceptanceTests;

public class ConfigureEndpointInMemoryTransport : IConfigureEndpointTestExecution
{
    public Task Cleanup() => Task.CompletedTask;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.EnforcePublisherMetadataRegistration(endpointName, publisherMetadata);
        configuration.LimitMessageProcessingConcurrencyTo(PushRuntimeSettings.Default.MaxConcurrency);
#pragma warning disable CS0618 // TODO we need a better way to do this
        configuration.RegisterComponents(services => services.TryAddSingleton(NServiceBusAcceptanceTest.CurrentBroker));
#pragma warning restore CS0618

        configuration.UseTransport(new InMemoryTransport());

        return Task.CompletedTask;
    }
}