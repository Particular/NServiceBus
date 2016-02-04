using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Transports;
using EndpointConfiguration = NServiceBus.EndpointConfiguration;

public class ConfigureMsmqTransport : IConfigureTestExecution
{
    EndpointConfiguration endpointConfiguration;

    public Task Configure(EndpointConfiguration configuration, IDictionary<string, string> settings)
    {
        endpointConfiguration = configuration;
        configuration.UseTransport<MsmqTransport>().ConnectionString(settings["Transport.ConnectionString"]);
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        var bindings = endpointConfiguration.GetSettings().Get<QueueBindings>();
        var allQueues = MessageQueue.GetPrivateQueuesByMachine("localhost");
        var queuesToBeDeleted = new List<string>();

        foreach (var messageQueue in allQueues)
        {
            using (messageQueue)
            {
                if (bindings.ReceivingAddresses.Any(ra =>
                {
                    var indexOfAt = ra.IndexOf("@", StringComparison.Ordinal);
                    if (indexOfAt >= 0)
                    {
                        ra = ra.Substring(0, indexOfAt);
                    }
                    return messageQueue.QueueName.StartsWith(@"private$\" + ra, StringComparison.OrdinalIgnoreCase);
                }))
                {
                    queuesToBeDeleted.Add(messageQueue.Path);
                }
            }
        }

        foreach (var queuePath in queuesToBeDeleted)
        {
            try
            {
                MessageQueue.Delete(queuePath);
                Console.WriteLine("Deleted '{0}' queue", queuePath);
            }
            catch (Exception)
            {
                Console.WriteLine("Could not delete queue '{0}'", queuePath);                
            }
        }

        MessageQueue.ClearConnectionCache();

        return Task.FromResult(0);
    }
}
