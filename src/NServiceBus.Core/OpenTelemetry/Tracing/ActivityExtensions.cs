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
            && activity is { IsAllDataRequested: true }) // do not apply "expensive" work on non-recording activities
        {
            return true;
        }

        activity = null;
        return false;
    }

    public static void SetOutgoingPipelineActivity(this ContextBag pipelineContext, Activity activity) => pipelineContext.Set(OutgoingActivityKey, activity);
    public static void SetIncomingPipelineActivity(this ContextBag pipelineContext, Activity activity) => pipelineContext.Set(IncomingActivityKey, activity);

    // Activity.Stop() makes the activity that was current when Start() ran current again. To start a new trace,
    // ActivityFactory clears Activity.Current before starting the activity, so stopping that activity leaves
    // Activity.Current null instead of the activity that was current before, for example the transport's receive span.
    // Callers capture Activity.Current before starting the activity and pass it here once the activity is disposed.
    public static void RestoreAmbientActivity(Activity? ambientActivity)
    {
        // The runtime rejects a stopped activity as current, so Activity.Current is left as it is in that case.
        if (ambientActivity is { IsStopped: true })
        {
            return;
        }

        Activity.Current = ambientActivity;
    }
}