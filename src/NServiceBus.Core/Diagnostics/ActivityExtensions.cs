namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Extensibility;

static class ActivityExtensions
{
    public const string OutgoingActivityKey = "NServiceBus.Diagnostics.Activity.Outgoing";
    public const string IncomingActivityKey = "NServiceBus.Diagnostics.Activity.Incoming";

    public static bool TryGetRecordingOutgoingPipelineActivity(this ContextBag pipelineContext, out Activity activity)
        => pipelineContext.TryGetRecordingPipelineActivity(OutgoingActivityKey, out activity);
    public static bool TryGetRecordingIncomingPipelineActivity(this ContextBag pipelineContext, out Activity activity)
        => pipelineContext.TryGetRecordingPipelineActivity(IncomingActivityKey, out activity);

    static bool TryGetRecordingPipelineActivity(this ContextBag pipelineContext, string activityKey, out Activity activity)
    {
        if (Activity.Current != null // Cheaper to check than searching the pipeline context to start with. If there is no ambient activity, there can't be an activity in the context.
            && pipelineContext.TryGet(activityKey, out activity)  // Search activity in context bag
            && activity is { IsAllDataRequested: true }) // do not apply "expensive" work on non-recording activities
        {
            return true;
        }

        activity = null;
        return false;
    }

    public static void SetOutgoingPipelineActitvity(this ContextBag pipelineContext, Activity activity) => pipelineContext.Set(OutgoingActivityKey, activity);
    public static void SetIncomingPipelineActitvity(this ContextBag pipelineContext, Activity activity) => pipelineContext.Set(IncomingActivityKey, activity);

    public static void SetErrorStatus(this Activity activity, Exception ex)
    {
        //TODO: Do we want to truncate the exception message? or use the existing safe method `ex.GetMessage()`?
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.SetTag("otel.status_code", "ERROR");
        activity.SetTag("otel.status_description", ex.Message);
        activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow,
            new ActivityTagsCollection(new[]
            {
                new KeyValuePair<string, object>("exception.escaped", true),
                new KeyValuePair<string, object>("exception.type", ex.GetType()),
                new KeyValuePair<string, object>("exception.message", ex.Message),
                new KeyValuePair<string, object>("exception.stacktrace", ex.ToString()),
            })));

        if (ex is TaskCanceledException)
        {
            activity.SetTag(ActivityTags.CancelledTask, true);
        }
    }
}