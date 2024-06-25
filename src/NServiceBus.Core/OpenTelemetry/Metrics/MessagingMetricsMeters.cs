namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

class MessagingMetricsMeters
{
    public MessagingMetricsMeters(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("NServiceBus.Core", "0.2.0");
        totalProcessedSuccessfully = meter.CreateCounter<long>(Metrics.TotalProcessedSuccessfully,
            description: "Total number of messages processed successfully by the endpoint.");
        totalFetched = meter.CreateCounter<long>(Metrics.TotalFetched,
            description: "Total number of messages fetched from the queue by the endpoint.");
        totalFailures = meter.CreateCounter<long>(Metrics.TotalFailures,
            description: "Total number of messages processed unsuccessfully by the endpoint.");
        messageHandlerTime = meter.CreateHistogram<double>(Metrics.MessageHandlerTime, "s",
            "The time in seconds for the execution of the business code.");
        criticalTime = meter.CreateHistogram<double>(Metrics.CriticalTime, "s",
            "The time in seconds between when the message was sent until processed by the endpoint.");
    }

    public void RecordMessageSuccessfullyProcessed(TagList tags) => totalProcessedSuccessfully.Add(1, tags);
    public void RecordMessageProcessingFailure(TagList tags) => totalFailures.Add(1, tags);
    public void RecordFetchedMessage(TagList tags) => totalFetched.Add(1, tags);
    public void RecordMessageCriticalTime(TimeSpan messageCriticalTime, TagList tags) => criticalTime.Record(messageCriticalTime.TotalSeconds, tags);
    public void RecordMessageHandlerTime(TimeSpan messageHandlerTimeSpan, TagList tags) => messageHandlerTime.Record(messageHandlerTimeSpan.TotalSeconds, tags);

    readonly Counter<long> totalProcessedSuccessfully;
    readonly Counter<long> totalFetched;
    readonly Counter<long> totalFailures;
    readonly Histogram<double> messageHandlerTime;
    readonly Histogram<double> criticalTime;
}