#nullable enable

namespace NServiceBus;

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Logging;

class DefaultLoggerFactory(LogLevel filterLevel, string loggingDirectory) : ILoggerFactory
{
    public ILog GetLogger(Type type) => GetLogger(type.FullName!);

    public ILog GetLogger(string name) =>
        new NamedLogger(name, this)
        {
            IsDebugEnabled = isDebugEnabled,
            IsInfoEnabled = isInfoEnabled,
            IsWarnEnabled = isWarnEnabled,
            IsErrorEnabled = isErrorEnabled,
            IsFatalEnabled = isFatalEnabled
        };

#pragma warning disable IDE0060 // Remove unused parameter
    public void Write(string name, LogLevel messageLevel, string message, Exception? exception = null)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        if (messageLevel < filterLevel)
        {
            return;
        }

        var stringBuilder = new StringBuilder();
#pragma warning disable PS0023 // Use DateTime.UtcNow or DateTimeOffset.UtcNow - For logging
        var datePart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
#pragma warning restore PS0023 // Use DateTime.UtcNow or DateTimeOffset.UtcNow
        var paddedLevel = messageLevel.ToString().ToUpper().PadRight(5);

        stringBuilder.Append(datePart).Append(' ').Append(paddedLevel).Append(' ').Append(message);

        if (exception != null)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append(exception);
            if (exception.Data.Count > 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("Exception details:");

                foreach (DictionaryEntry exceptionData in exception.Data)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append('\t').Append(exceptionData.Key).Append(": ").Append(exceptionData.Value);
                }
            }
        }

        var fullMessage = stringBuilder.ToString();
        lock (locker)
        {
            rollingLogger.WriteLine(fullMessage);
            ColoredConsoleLogger.WriteLine(fullMessage, messageLevel);
            Trace.WriteLine(fullMessage);
        }
    }

    readonly bool isDebugEnabled = filterLevel <= LogLevel.Debug;
    readonly bool isErrorEnabled = filterLevel <= LogLevel.Error;
    readonly bool isFatalEnabled = filterLevel <= LogLevel.Fatal;
    readonly bool isInfoEnabled = filterLevel <= LogLevel.Info;
    readonly bool isWarnEnabled = filterLevel <= LogLevel.Warn;

    readonly Lock locker = new();
    readonly RollingLogger rollingLogger = new(loggingDirectory);
}