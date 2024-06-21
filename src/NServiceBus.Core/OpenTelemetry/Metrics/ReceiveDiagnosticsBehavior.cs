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
        var metricTags = context.Extensions.Get<Dictionary<string, object>>(MetricTagsExtensions.AvailableMetricsTags);
        metricTags.Add(MeterTags.EndpointDiscriminator, discriminator);
        metricTags.Add(MeterTags.QueueName, queueNameBase);

        var tags = new TagList();
        metricTags.Apply(ref tags, MeterTags.EndpointDiscriminator, MeterTags.QueueName);
        Meters.TotalFetched.Add(1, tags);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
        {
            metricTags.Apply(ref tags, MeterTags.MessageType, MeterTags.MessageHandlerTypes);
            tags.Add(new(MeterTags.FailureType, ex.GetType()));
            Meters.TotalFailures.Add(1, tags);
            throw;
        }

        metricTags.Apply(ref tags, MeterTags.MessageType, MeterTags.MessageHandlerTypes);
        Meters.TotalProcessedSuccessfully.Add(1, tags);
    }

    readonly string queueNameBase;
    readonly string discriminator;
}