namespace NServiceBus.Faults;

using System;

/// <summary>
/// Class holding keys to message headers for faults.
/// </summary>
public static class FaultsHeaderKeys
{
    /// <summary>
    /// Header key for setting/getting the queue at which the message processing failed.
    /// </summary>
    public const string FailedQ = "NServiceBus.FailedQ";

    /// <summary>
    /// Header key for setting/getting the exception type that caused the message processing to fail.
    /// </summary>
    public const string ExceptionType = $"{ExceptionInfoPrefix}ExceptionType";

    /// <summary>
    /// Header key for setting/getting the inner exception type that caused the message processing to fail.
    /// </summary>
    public const string InnerExceptionType = $"{ExceptionInfoPrefix}InnerExceptionType";

    /// <summary>
    /// Header key for setting/getting the <see cref="Exception.HelpLink"/> of the exception that caused the message processing to fail.
    /// </summary>
    public const string HelpLink = $"{ExceptionInfoPrefix}HelpLink";

    /// <summary>
    /// Header key for setting/getting the <see cref="Exception.Message"/> of the exception that caused the message processing to fail.
    /// </summary>
    public const string Message = $"{ExceptionInfoPrefix}Message";

    /// <summary>
    /// Header key for setting/getting the <see cref="Exception.Source"/> of the exception that caused the message processing to fail.
    /// </summary>
    public const string Source = $"{ExceptionInfoPrefix}Source";

    /// <summary>
    /// Header key for setting/getting the <see cref="Exception.StackTrace"/> of the exception that caused the message processing to fail.
    /// </summary>
    public const string StackTrace = $"{ExceptionInfoPrefix}StackTrace";

    /// <summary>
    /// Header key for setting/getting the normalized time of the failure.
    /// </summary>
    public const string TimeOfFailure = "NServiceBus.TimeOfFailure";

    const string ExceptionInfoPrefix = "NServiceBus.ExceptionInfo.";
    internal const string ExceptionInfoDataPrefix = $"{ExceptionInfoPrefix}Data.";
}