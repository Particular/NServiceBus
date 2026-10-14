#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Extensibility;

static class ActivityExtensions
{
    public const string OutgoingActivityKey = "NServiceBus.Diagnostics.Activity.Outgoing";
    public const string IncomingActivityKey = "NServiceBus.Diagnostics.Activity.Incoming";
    public const string RecordedExceptionKey = "NServiceBus.Diagnostics.RecordedException";
    public const string FailedAttemptKey = "NServiceBus.Diagnostics.FailedAttempt";

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

    // The innermost span that fails records the exception. Outer spans see the same instance propagate and skip it.
    public static bool IsRecordedOnSpan(this ContextBag pipelineContext, Exception exception) =>
        pipelineContext.TryGet<RecordedException>(RecordedExceptionKey, out var recorded) && ReferenceEquals(recorded.Exception, exception);

    public static void MarkAsRecordedOnSpan(this ContextBag pipelineContext, Exception exception, Activity activity) =>
        pipelineContext.SetOnRoot(RecordedExceptionKey, new RecordedException(exception, activity.Context));

    // Transports pass this ContextBag to ErrorContext, enforced by the When_setting_context_items transport test.
    // The marker is removed because some transports reuse the ContextBag for the next attempt.
    public static void SetFailedAttempt(this ContextBag transportContext, Activity incomingPipelineActivity, Exception exception)
    {
        var innermostFailedActivityContext = transportContext.TryGet<RecordedException>(RecordedExceptionKey, out var recorded) && ReferenceEquals(recorded.Exception, exception)
            ? recorded.ActivityContext
            : incomingPipelineActivity.Context;

        transportContext.Remove(RecordedExceptionKey);
        transportContext.Set(FailedAttemptKey, new FailedAttempt(incomingPipelineActivity.Context, innermostFailedActivityContext));
    }

    public static bool TryGetFailedAttempt(this ContextBag transportContext, [NotNullWhen(true)] out FailedAttempt? failedAttempt) =>
        transportContext.TryGet(FailedAttemptKey, out failedAttempt);
}

sealed record RecordedException(Exception Exception, ActivityContext ActivityContext);

sealed record FailedAttempt(ActivityContext IncomingPipelineActivityContext, ActivityContext InnermostFailedActivityContext);
