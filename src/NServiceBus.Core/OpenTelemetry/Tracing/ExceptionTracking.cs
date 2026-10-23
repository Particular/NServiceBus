#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using Extensibility;

sealed class ExceptionTracking
{
    public static ExceptionTracking For(ContextBag contextBag)
    {
        if (contextBag.TryGet(Key, out ExceptionTracking? tracking))
        {
            return tracking;
        }

        tracking = new ExceptionTracking();
        contextBag.SetOnRoot(Key, tracking);
        return tracking;
    }

    public bool TryRecordExceptionEvent(Exception exception)
    {
        if (ReferenceEquals(exceptionWithEvent, exception))
        {
            return false;
        }

        exceptionWithEvent = exception;
        return true;
    }

    public void RecordFailedActivity(Exception exception, Activity activity)
    {
        if (ReferenceEquals(exceptionWithFailedActivity, exception))
        {
            return;
        }

        exceptionWithFailedActivity = exception;
        failedActivityContext = activity.Context;
    }

    public bool TryGetFailedActivityContext(Exception exception, out ActivityContext activityContext)
    {
        activityContext = ReferenceEquals(exceptionWithFailedActivity, exception) ? failedActivityContext : default;
        return activityContext != default;
    }

    Exception? exceptionWithEvent;
    Exception? exceptionWithFailedActivity;
    ActivityContext failedActivityContext;

    static readonly string Key = typeof(ExceptionTracking).FullName!;
}
