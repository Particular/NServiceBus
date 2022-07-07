namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Threading.Tasks;
    using Pipeline;

    class ReceiveDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        //TODO review tag names
        static readonly Counter<long> TotalProcessedSuccessfully =
            MessagingMetricsFeature.NServiceBusMeter.CreateCounter<long>("nservicebus.messaging.successes", description: "Total number of messages processed successfully by the endpoint.");

        static readonly Counter<long> TotalFetched =
            MessagingMetricsFeature.NServiceBusMeter.CreateCounter<long>("nservicebus.messaging.fetches", description: "Total number of messages fetched from the queue by the endpoint.");

        static readonly Counter<long> TotalFailures =
            MessagingMetricsFeature.NServiceBusMeter.CreateCounter<long>("nservicebus.messaging.failures", description: "Total number of messages processed unsuccessfully by the endpoint.");

        public ReceiveDiagnosticsBehavior(string queueNameBase, string discriminator)
        {
            this.queueNameBase = queueNameBase;
            this.discriminator = discriminator;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            context.MessageHeaders.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypes);

            //TODO review tag names
            var tags = new List<KeyValuePair<string, object>>
            {
                new("nservicebus.discriminator", discriminator ?? ""),
                new("nservicebus.queue", queueNameBase ?? ""),
                new("nservicebus.type", messageTypes ?? ""),
            };

            TotalFetched.Add(1, tags.ToArray());

            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
            {
                tags.Add(new KeyValuePair<string, object>("nservicebus.failure_type", ex.GetType()));
                TotalFailures.Add(1, tags.ToArray());
                throw;
            }

            TotalProcessedSuccessfully.Add(1, tags.ToArray());
        }

        readonly string queueNameBase;
        readonly string discriminator;
    }
}