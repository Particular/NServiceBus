#nullable enable

namespace NServiceBus;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Extensibility;

static class ActivityExtensions
{
    public const string OutgoingActivityKey = "NServiceBus.Diagnostics.Activity.Outgoing";
    public const string IncomingActivityKey = "NServiceBus.Diagnostics.Activity.Incoming";

    public static bool TryGetOutgoingPipelineActivity(this ContextBag pipelineContext, [NotNullWhen(true)] out Activity? activity)
        => pipelineContext.TryGetPipelineActivity(OutgoingActivityKey, out activity);

    public static bool TryGetIncomingPipelineActivity(this ContextBag pipelineContext, [NotNullWhen(true)] out Activity? activity)
        => pipelineContext.TryGetPipelineActivity(IncomingActivityKey, out activity);

    static bool TryGetPipelineActivity(this ContextBag pipelineContext, string activityKey, [NotNullWhen(true)] out Activity? activity)
    {
        if (Activity.Current is not null // Cheaper to check than searching the pipeline context to start with. If there is no ambient activity, there can't be an activity in the context.
            && pipelineContext.TryGet(activityKey, out activity)  // Search activity in context bag
            && activity is { IsAllDataRequested: true }) // skip expensive work when no listener asked for full data (IsAllDataRequested, not Recorded/sampled)
        {
            return true;
        }

        activity = null;
        return false;
    }

    public static void SetOutgoingPipelineActivity(this ContextBag pipelineContext, Activity activity) => pipelineContext.Set(OutgoingActivityKey, activity);
    public static void SetIncomingPipelineActivity(this ContextBag pipelineContext, Activity activity) => pipelineContext.Set(IncomingActivityKey, activity);
}