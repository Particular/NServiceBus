using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;

public class ConfigureMsmqTransport
{
    BusConfiguration busConfiguration;

    public Task Configure(BusConfiguration configuration)
    {
        busConfiguration = configuration;
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        var name = busConfiguration.GetSettings().EndpointName();
        var nameFilter = @"private$\" + name;
        var allQueues = MessageQueue.GetPrivateQueuesByMachine("localhost");
        var queuesToBeDeleted = new List<string>();

        foreach (var messageQueue in allQueues)
        {
            using (messageQueue)
            {
                if (messageQueue.QueueName.StartsWith(nameFilter, StringComparison.OrdinalIgnoreCase))
                {
                    queuesToBeDeleted.Add(messageQueue.Path);
                }
            }
        }

        foreach (var queuePath in queuesToBeDeleted)
        {
            MessageQueue.Delete(queuePath);
            Console.WriteLine("Deleted '{0}' queue", queuePath);
        }

        MessageQueue.ClearConnectionCache();

        return Task.FromResult(0);
    }
}
