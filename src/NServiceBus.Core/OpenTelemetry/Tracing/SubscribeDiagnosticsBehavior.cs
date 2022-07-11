namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class SubscribeDiagnosticsBehavior : IBehavior<ISubscribeContext, ISubscribeContext>
{
    public Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
    {
        if (context.Extensions.TryGetRecordingOutgoingPipelineActivity(out var activity))
        {
            activity?.SetTag(ActivityTags.EventTypes, string.Join(",", (object[])context.EventTypes));
        }

        return next(context);
    }
}