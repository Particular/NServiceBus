using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Legacy;


public class ConfigureMsmqPersistence : IConfigureTestExecution
{
    public Task Configure(BusConfiguration configuration, IDictionary<string, string> settings)
    {
        configuration.UsePersistence<InMemoryPersistence>();
        configuration.UsePersistence<MsmqPersistence>();
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        // Nothing required for in-memory persistence
        return Task.FromResult(0);
    }
}