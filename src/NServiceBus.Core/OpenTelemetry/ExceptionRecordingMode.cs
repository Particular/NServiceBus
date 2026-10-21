#nullable enable

namespace NServiceBus;

/// <summary>
/// Controls how exception details are recorded when an operation represented by an activity fails. The exception
/// is always logged once recoverability has decided what to do with the message; the mode controls whether it is
/// also recorded as an event on the activity the exception escaped from.
/// </summary>
public enum ExceptionRecordingMode
{
    /// <summary>
    /// Records the exception details only in the log entry written when recoverability decides.
    /// </summary>
    Logs,

    /// <summary>
    /// Records the exception details in the log entry written when recoverability decides and as an event on the
    /// activity the exception escaped from.
    /// </summary>
    SpanAndLogs
}