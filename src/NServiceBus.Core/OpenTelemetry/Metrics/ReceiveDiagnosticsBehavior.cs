namespace NServiceBus;

using System;
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
        var availableMetricTags = context.Extensions.Get<IncomingPipelineMetricTags>();
        availableMetricTags.Add(MeterTags.EndpointDiscriminator, discriminator);
        availableMetricTags.Add(MeterTags.QueueName, queueNameBase);

        var tags = new TagList();
        availableMetricTags.ApplyTags(ref tags, [MeterTags.EndpointDiscriminator, MeterTags.QueueName]);
        Meters.TotalFetched.Add(1, tags);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
        {
            tags.Add(new(MeterTags.FailureType, ex.GetType()));
            availableMetricTags.ApplyTags(ref tags, [MeterTags.MessageType, MeterTags.MessageHandlerTypes]);
            Meters.TotalFailures.Add(1, tags);
            throw;
        }

        availableMetricTags.ApplyTags(ref tags, [MeterTags.MessageType, MeterTags.MessageHandlerTypes]);
        Meters.TotalProcessedSuccessfully.Add(1, tags);
    }

    readonly string queueNameBase;
    readonly string discriminator;
}