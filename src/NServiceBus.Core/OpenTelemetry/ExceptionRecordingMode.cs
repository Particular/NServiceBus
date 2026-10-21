#nullable enable

namespace NServiceBus;

using Particular.Obsoletes;

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
    // In v11 SpanAndLogs is obsolete with an error and removed in v12. Then delete the SpanAndLogs paths in
    // ActivityFactory.RecordError and RecoverabilityActionLogger.LogRecoverabilityAction, so Logs is the only behavior.
    [PreObsolete("https://github.com/Particular/NServiceBus/pull/7984",
        Note = "In version 11, exception details are recorded only in the log. Recording them as span events is no longer available.",
        ReplacementTypeOrMember = "ExceptionRecordingMode.Logs")]
    SpanAndLogs
}
