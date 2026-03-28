using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting.Support;

namespace NServiceBus.AcceptanceTests;

public class ConfigureEndpointInMemoryPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.UseInMemoryPersistence();
        return Task.CompletedTask;
    }

    public Task Cleanup()
    {
        // Nothing required for in-memory persistence
        return Task.CompletedTask;
    }
}
