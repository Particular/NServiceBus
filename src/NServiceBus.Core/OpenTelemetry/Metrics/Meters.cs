namespace NServiceBus;

using System.Diagnostics.Metrics;

class Meters
{
    internal static readonly Meter NServiceBusMeter = new Meter(
        "NServiceBus.Core",
        "0.2.0");

    internal static readonly Counter<long> TotalProcessedSuccessfully =
        NServiceBusMeter.CreateCounter<long>("nservicebus.messaging.successes", description: "Total number of messages processed successfully by the endpoint.");

    internal static readonly Counter<long> TotalFetched =
        NServiceBusMeter.CreateCounter<long>("nservicebus.messaging.fetches", description: "Total number of messages fetched from the queue by the endpoint.");

    internal static readonly Counter<long> TotalFailures =
        NServiceBusMeter.CreateCounter<long>("nservicebus.messaging.failures", description: "Total number of messages processed unsuccessfully by the endpoint.");

    internal static readonly Histogram<double> ProcessingTime =
     NServiceBusMeter.CreateHistogram<double>("nservicebus.messaging.processingtime", "ms", "The time in milliseconds between when the message was pulled from the queue until processed by the endpoint.");

    internal static readonly Histogram<double> CriticalTime =
        NServiceBusMeter.CreateHistogram<double>("nservicebus.messaging.criticaltime", "ms", "The time in milliseconds between when the message was sent until processed by the endpoint.");
}