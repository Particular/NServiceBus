using System.Linq;
using System.Messaging;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;

public class ConfigureMsmqTransport
{
    BusConfiguration busConfiguration;

    public void Configure(BusConfiguration configuration)
    {
        busConfiguration = configuration;
    }

    public void Cleanup()
    {
        var name = busConfiguration.GetSettings().EndpointName();

        var queuesToBeDeleted = MessageQueue.GetPrivateQueuesByMachine("localhost").Where(n => n.QueueName.Contains(name.ToLowerInvariant()));
        foreach (MessageQueue queue in queuesToBeDeleted)
        {
            MessageQueue.Delete(queue.Path);
        }
    }
}
