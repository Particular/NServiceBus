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

    public bool TryRecordInnermostActivity(Exception exception, Activity activity)
    {
        if (ReferenceEquals(trackedException, exception))
        {
            return false;
        }

        trackedException = exception;
        innermostActivityContext = activity.Context;
        return true;
    }

    public bool TryGetInnermostActivityContext(Exception exception, out ActivityContext activityContext)
    {
        activityContext = ReferenceEquals(trackedException, exception) ? innermostActivityContext : default;
        return activityContext != default;
    }

    Exception? trackedException;
    ActivityContext innermostActivityContext;

    static readonly string Key = typeof(ExceptionTracking).FullName!;
}
