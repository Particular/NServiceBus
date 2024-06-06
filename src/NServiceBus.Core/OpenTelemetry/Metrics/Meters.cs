﻿namespace NServiceBus;

using System.Diagnostics.Metrics;

class Meters
{
    internal static readonly Meter NServiceBusMeter = new Meter(
        "NServiceBus.Core",
        "0.2.0");

    internal static readonly Counter<long> TotalProcessedSuccessfully =
        NServiceBusMeter.CreateCounter<long>(Metrics.TotalProcessedSuccessfully, description: "Total number of messages processed successfully by the endpoint.");

    internal static readonly Counter<long> TotalFetched =
        NServiceBusMeter.CreateCounter<long>(Metrics.TotalFetched, description: "Total number of messages fetched from the queue by the endpoint.");

    internal static readonly Counter<long> TotalFailures =
        NServiceBusMeter.CreateCounter<long>(Metrics.TotalFailures, description: "Total number of messages processed unsuccessfully by the endpoint.");

    internal static readonly Histogram<double> MessageHandlerTime =
        NServiceBusMeter.CreateHistogram<double>(Metrics.MessageHandlerTime, "ms", "The time in milliseconds for the execution of the business code.");
}