namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class EnableMetricTagsCollectionBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
{
    public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
    {
        var availableMetricTags = context.Extensions.Get<IncomingPipelineMetricTags>();
        availableMetricTags.CollectMetricTags();

        return next(context);
    }
}