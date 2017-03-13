using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureEndpointDevelopmentTransport : IConfigureEndpointTestExecution
{
    public Task Cleanup()
    {
        return Task.FromResult(0);
    }

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.UseTransport<DevelopmentTransport>();

        return Task.FromResult(0);
    }
}