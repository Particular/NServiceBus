#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using Extensibility;

// What the tracing has done with the exceptions of one message or one operation: which exception got its exception
// event, and which pipeline activity an exception escaped from. Kept on the root of the context bag, the one the
// transport floats from OnMessage to OnError, and keyed by exception instance: an exception thrown while another was
// handled is a new exception, and a context bag reused for the next attempt holds a different instance.
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

    // The innermost activity records first; the activities further out keep its context.
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
