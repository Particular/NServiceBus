#nullable enable

namespace NServiceBus;

/// <summary>
/// Controls how exception details are recorded when an operation represented by an activity fails.
/// </summary>
public enum ExceptionRecordingMode
{
    /// <summary>
    /// Records the exception details via NServiceBus's logging infrastructure instead of adding an event to the
    /// activity.
    /// </summary>
    Logs,

    /// <summary>
    /// Records the exception details via NServiceBus's logging infrastructure and as an event the activity.
    /// </summary>
    SpanAndLogs
}
