using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence;

public class ConfigureEndpointInMemoryPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.UsePersistence<InMemoryPersistence>();

        configuration.UsePersistence<DevelopmentPersistence, StorageType.Sagas>()
            .SagaStorageDirectory(@"c:\temp\sagas"); //todo: for now to avoid path to long on the build agents
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        // Nothing required for in-memory persistence
        return Task.FromResult(0);
    }
}