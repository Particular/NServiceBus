#nullable enable

namespace NServiceBus;

using System;
using System.Linq;
using System.Threading.Tasks;
using Pipeline;

class SubscribeDiagnosticsBehavior : IBehavior<ISubscribeContext, ISubscribeContext>
{
    public Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
    {
        if (context.Extensions.TryGetOutgoingPipelineActivity(out var activity))
        {
            // An array of full type names: the OpenTelemetry naming rules ask for an array when an attribute holds
            // several values. An array-valued tag is only visible through Activity.TagObjects, not Activity.Tags.
            // Keep only the v11 branch in v11, see obsoletes-v10.cs.
            if (V11BehaviorSwitch.UseV11Behavior)
            {
                activity.SetTag(ActivityTags.EventTypes, context.EventTypes.Select(static type => type.FullName).ToArray());
            }
            else
            {
                activity.SetTag(ActivityTags.EventTypes, string.Join(",", (object[])context.EventTypes));
            }
        }

        return next(context);
    }
}