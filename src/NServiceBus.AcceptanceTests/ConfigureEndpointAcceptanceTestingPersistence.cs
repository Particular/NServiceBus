namespace NServiceBus.AcceptanceTests;

using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureEndpointAcceptanceTestingPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        // Compiled into test projects whose other endpoints use sagas with timeout management, which the analyzer can't tell apart
#pragma warning disable NSB0044
        configuration.UsePersistence<AcceptanceTestingPersistence>();
#pragma warning restore NSB0044
        return Task.CompletedTask;
    }

    public Task Cleanup()
    {
        // Nothing required for in-memory persistence
        return Task.CompletedTask;
    }
}