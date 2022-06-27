namespace NServiceBus;

using System.Diagnostics;
using Extensibility;

static class ActivityExtensions
{
    public const string OutgoingActivityKey = "NServiceBus.Diagnostics.Activity.Outgoing";

    public static bool TryGetRecordingPipelineActivity(this ContextBag pipelineContext, out Activity activity)
    {
        if (Activity.Current != null // Cheaper to check than searching the pipeline context to start with. If there is no ambient activity, there can't be a context in the context.
            && pipelineContext.TryGet(OutgoingActivityKey, out activity)  // Search activity in context bag
            && activity.IsAllDataRequested) // do not apply "expensive" work on non-recording activities
        {
            return true;
        }

        activity = null;
        return false;
    }

    public static void SetPipelineActitvity(this ContextBag pipelineContext, Activity activity) => pipelineContext.Set(OutgoingActivityKey, activity);
}