namespace NServiceBus;

using System.Diagnostics.Metrics;

class ReceiveDiagnosticsMeters
{
    internal static readonly Counter<long> TotalProcessedSuccessfully =
        Meters.NServiceBusMeter.CreateCounter<long>(Metrics.TotalProcessedSuccessfully,
            description: "Total number of messages processed successfully by the endpoint.");

    internal static readonly Counter<long> TotalFetched = Meters.NServiceBusMeter.CreateCounter<long>(
        Metrics.TotalFetched, description: "Total number of messages fetched from the queue by the endpoint.");

    internal static readonly Counter<long> TotalFailures = Meters.NServiceBusMeter.CreateCounter<long>(
        Metrics.TotalFailures, description: "Total number of messages processed unsuccessfully by the endpoint.");
}