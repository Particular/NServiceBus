namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;

class ReceiveDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
{
    public ReceiveDiagnosticsBehavior(string queueNameBase, string discriminator)
    {
        this.queueNameBase = queueNameBase;
        this.discriminator = discriminator;
    }

    public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        var tags = new TagList(new KeyValuePair<string, object>[]
        {
            new(MeterTags.EndpointDiscriminator, discriminator ?? ""),
            new(MeterTags.QueueName, queueNameBase ?? ""),
        }.AsSpan());

        Meters.TotalFetched.Add(1, tags);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
        {
            var onFailureMetricTags = context.Extensions.Get<MetricTags>();
            onFailureMetricTags.AddMessageTypeIfExists(ref tags);
            onFailureMetricTags.AddMessageHandlerTypesIfExists(ref tags);
            tags.Add(new(MeterTags.FailureType, ex.GetType()));
            Meters.TotalFailures.Add(1, tags);
            throw;
        }

        var metricTags = context.Extensions.Get<MetricTags>();
        metricTags.AddMessageTypeIfExists(ref tags);
        metricTags.AddMessageHandlerTypesIfExists(ref tags);
        Meters.TotalProcessedSuccessfully.Add(1, tags);
    }

    readonly string queueNameBase;
    readonly string discriminator;
}