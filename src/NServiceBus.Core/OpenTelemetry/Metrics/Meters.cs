﻿namespace NServiceBus;

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

    internal static readonly Counter<long> TotalImmediateRetries =
        NServiceBusMeter.CreateCounter<long>("nservicebus.recoverability.immediate", description: "Total number of immediate retries requested.");

    internal static readonly Counter<long> TotalDelayedRetries =
        NServiceBusMeter.CreateCounter<long>("nservicebus.recoverability.delayed", description: "Total number of delayed retries requested.");

    internal static readonly Counter<long> TotalSentToErrorQueue =
        NServiceBusMeter.CreateCounter<long>("nservicebus.recoverability.error", description: "Total number of messages sent to the error queue.");
}