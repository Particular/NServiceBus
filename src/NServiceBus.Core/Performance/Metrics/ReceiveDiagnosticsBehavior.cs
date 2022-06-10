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
            NServiceBusMeter.CreateCounter<long>("messaging.successes", "Total number of messages processed successfully by the endpoint.");

        static readonly Counter<long> TotalFetched =
            NServiceBusMeter.CreateCounter<long>("messaging.fetches", "Total number of messages fetched from the queue by the endpoint.");

        static readonly Counter<long> TotalFailures =
            NServiceBusMeter.CreateCounter<long>("messaging.failures", "Total number of messages processed unsuccessfully by the endpoint.");

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            context.MessageHeaders.TryGetMessageType(out var _);

            //TODO: do we need to tag the message type, or any other data? 
            var tags = Array.Empty<KeyValuePair<string, object>>();

            TotalFetched.Add(1, tags);

            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
            {
                TotalFailures.Add(1, tags);
                throw;
            }

            TotalProcessedSuccessfully.Add(1, tags);
        }
    }
}