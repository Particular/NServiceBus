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
        var metricTags = context.Extensions.Get<MetricTags>();
        metricTags.EndpointDiscriminator = discriminator;
        metricTags.QueueName = queueNameBase;

        var tags = new TagList();
        metricTags.AddEndpointDiscriminatorIfExists(ref tags);
        metricTags.AddQueueNameIfExists(ref tags);
        Meters.TotalFetched.Add(1, tags);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
        {
            metricTags.AddMessageTypeIfExists(ref tags);
            metricTags.AddMessageHandlerTypesIfExists(ref tags);
            tags.Add(new(MeterTags.FailureType, ex.GetType()));
            Meters.TotalFailures.Add(1, tags);
            throw;
        }

        metricTags.AddMessageTypeIfExists(ref tags);
        metricTags.AddMessageHandlerTypesIfExists(ref tags);
        Meters.TotalProcessedSuccessfully.Add(1, tags);
    }

    readonly string queueNameBase;
    readonly string discriminator;
}