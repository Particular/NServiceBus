using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using EndpointConfiguration = NServiceBus.EndpointConfiguration;


public class ConfigureInMemoryPersistence : IConfigureTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new Type[0];

    public Task Configure(EndpointConfiguration configuration, IDictionary<string, string> settings)
    {
        configuration.UsePersistence<InMemoryPersistence>();
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        // Nothing required for in-memory persistence
        return Task.FromResult(0);
    }
}