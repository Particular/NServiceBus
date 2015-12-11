using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;


public class ConfigureInMemoryPersistence
{
    public Task Configure(BusConfiguration configuration, IDictionary<string, string> settings)
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