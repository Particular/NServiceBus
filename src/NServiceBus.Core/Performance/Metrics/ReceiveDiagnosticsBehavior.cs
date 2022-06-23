namespace NServiceBus.Performance.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Threading.Tasks;
    using Pipeline;

    class ReceiveDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        static readonly Meter NServiceBusMeter = new Meter(
            NServiceBusDiagnosticsInfo.InstrumentationName,
            NServiceBusDiagnosticsInfo.InstrumentationVersion);

        static readonly Counter<long> TotalProcessedSuccessfully =
            NServiceBusMeter.CreateCounter<long>("messaging.successes", description: "Total number of messages processed successfully by the endpoint.");

        static readonly Counter<long> TotalFetched =
            NServiceBusMeter.CreateCounter<long>("messaging.fetches", description: "Total number of messages fetched from the queue by the endpoint.");

        static readonly Counter<long> TotalFailures =
            NServiceBusMeter.CreateCounter<long>("messaging.failures", description: "Total number of messages processed unsuccessfully by the endpoint.");

        public ReceiveDiagnosticsBehavior(string endpointName, string queueNameBase, string discriminator)
        {
            this.endpointName = endpointName;
            this.queueNameBase = queueNameBase;
            this.discriminator = discriminator;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            context.MessageHeaders.TryGetMessageType(out var messageTypes);

            var tags = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("messaging.endpoint", endpointName),
                new KeyValuePair<string, object>("messaging.discriminator", discriminator),
                new KeyValuePair<string, object>("messaging.queue", queueNameBase),
                new KeyValuePair<string, object>("messaging.type", messageTypes),
            };

            TotalFetched.Add(1, tags.ToArray());

            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
            {
                tags.Add(new KeyValuePair<string, object>("messaging.failure_type", ex.GetType()));
                TotalFailures.Add(1, tags.ToArray());
                throw;
            }

            TotalProcessedSuccessfully.Add(1, tags.ToArray());
        }

        readonly string endpointName;
        readonly string queueNameBase;
        readonly string discriminator;
    }
}