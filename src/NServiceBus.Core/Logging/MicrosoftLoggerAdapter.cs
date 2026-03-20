#nullable enable
namespace NServiceBus;

using System;
using Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

sealed class MicrosoftLoggerAdapter(ILogger logger) : ILog
{
    static readonly Func<string?, Exception?, string> MessageFormatter = static (state, _) => state ?? string.Empty;
    static readonly Func<(string format, object?[] args), Exception?, string> FormatMessageFormatter = static (state, _) => string.Format(state.format, state.args);

    public bool IsDebugEnabled => logger.IsEnabled(LogLevel.Debug);
    public bool IsInfoEnabled => logger.IsEnabled(LogLevel.Information);
    public bool IsWarnEnabled => logger.IsEnabled(LogLevel.Warning);
    public bool IsErrorEnabled => logger.IsEnabled(LogLevel.Error);
    public bool IsFatalEnabled => logger.IsEnabled(LogLevel.Critical);

    public void Debug(string? message) => Write(LogLevel.Debug, message);

    public void Debug(string? message, Exception? exception) => Write(LogLevel.Debug, message, exception);

    public void DebugFormat(string format, params object?[] args) => WriteFormat(LogLevel.Debug, format, args);

    public void Info(string? message) => Write(LogLevel.Information, message);

    public void Info(string? message, Exception? exception) => Write(LogLevel.Information, message, exception);

    public void InfoFormat(string format, params object?[] args) => WriteFormat(LogLevel.Information, format, args);

    public void Warn(string? message) => Write(LogLevel.Warning, message);

    public void Warn(string? message, Exception? exception) => Write(LogLevel.Warning, message, exception);

    public void WarnFormat(string format, params object?[] args) => WriteFormat(LogLevel.Warning, format, args);

    public void Error(string? message) => Write(LogLevel.Error, message);

    public void Error(string? message, Exception? exception) => Write(LogLevel.Error, message, exception);

    public void ErrorFormat(string format, params object?[] args) => WriteFormat(LogLevel.Error, format, args);

    public void Fatal(string? message) => Write(LogLevel.Critical, message);

    public void Fatal(string? message, Exception? exception) => Write(LogLevel.Critical, message, exception);

    public void FatalFormat(string format, params object?[] args) => WriteFormat(LogLevel.Critical, format, args);

    void Write(LogLevel level, string? message, Exception? exception = null)
    {
        if (!logger.IsEnabled(level))
        {
            return;
        }

        logger.Log(level, default, message, exception, MessageFormatter);
    }

    void WriteFormat(LogLevel level, string format, object?[] args)
    {
        if (!logger.IsEnabled(level))
        {
            return;
        }

        logger.Log(level, default, (format, args), null, FormatMessageFormatter);
    }
}