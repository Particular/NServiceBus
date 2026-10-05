#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class UnsubscribeDiagnosticsBehavior : IBehavior<IUnsubscribeContext, IUnsubscribeContext>
{
    public Task Invoke(IUnsubscribeContext context, Func<IUnsubscribeContext, Task> next)
    {
        if (context.Extensions.TryGetOutgoingPipelineActivity(out var activity))
        {
            activity.SetTag(ActivityTags.EventTypes, context.EventType.FullName);
        }

        return next(context);
    }
}