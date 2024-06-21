namespace NServiceBus;

using System.Diagnostics.Metrics;

class PipelineMeters
{
    internal static readonly Histogram<double> MessageHandlerTime =
        Meters.NServiceBusMeter.CreateHistogram<double>(Metrics.MessageHandlerTime, "s",
            "The time in seconds for the execution of the business code.");

    internal static readonly Histogram<double> CriticalTime =
        Meters.NServiceBusMeter.CreateHistogram<double>(Metrics.CriticalTime, "s",
            "The time in seconds between when the message was sent until processed by the endpoint.");

    internal static readonly Histogram<double> ProcessingTime =
        Meters.NServiceBusMeter.CreateHistogram<double>(Metrics.ProcessingTime, "s",
            "The time in seconds since the message was fetched from the input queue until processed by the endpoint.");
}