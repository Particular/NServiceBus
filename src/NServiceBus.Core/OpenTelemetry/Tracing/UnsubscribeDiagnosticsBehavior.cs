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
            // A single-element array, so the tag has the same shape as on the subscribe span.
            // Keep only the v11 branch in v11, see obsoletes-v10.cs.
            if (V11BehaviorSwitch.UseV11Behavior)
            {
                activity.SetTag(ActivityTags.EventTypes, new[] { context.EventType.FullName });
            }
            else
            {
                activity.SetTag(ActivityTags.EventTypes, context.EventType.FullName);
            }
        }

        return next(context);
    }
}