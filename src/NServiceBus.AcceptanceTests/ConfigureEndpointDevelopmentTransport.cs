using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;


public class ConfigureEndpointDevelopmentTransport : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        //todo: use a path local to the test dir
        configuration.UseTransport<DevelopmentTransport>();
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        //todo: cleanup the test dir
        return Task.FromResult(0);
    }
}
