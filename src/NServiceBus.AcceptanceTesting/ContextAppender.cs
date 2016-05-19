namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Diagnostics;
    using Logging;

    /// <summary>
    /// This class is written under the assumption that acceptance tests are executed sequentially.
    /// </summary>
    class ContextAppender : ILog
    {
        public ContextAppender(string name, LogLevel level, Func<ScenarioContext> contextGetter)
        {
            this.contextGetter = contextGetter;
            this.level = level;
        }
        
        public bool IsDebugEnabled => level <= LogLevel.Debug;
        public bool IsInfoEnabled => level <= LogLevel.Info;
        public bool IsWarnEnabled => level <= LogLevel.Warn;
        public bool IsErrorEnabled => level <= LogLevel.Error;
        public bool IsFatalEnabled => level <= LogLevel.Fatal;


        void AppendException(Exception exception)
        {
            contextGetter()?.LoggedExceptions.Enqueue(exception);
        }

        public void Debug(string message)
        {
            WriteToTrace(message, LogLevel.Debug);
        }

        public void Debug(string message, Exception exception)
        {
            AppendException(exception);
            WriteToTrace(message, LogLevel.Debug);
        }

        public void DebugFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            WriteToTrace(fullMessage, LogLevel.Debug);
        }

        public void Info(string message)
        {
            WriteToTrace(message, LogLevel.Info);
        }


        public void Info(string message, Exception exception)
        {
            var fullMessage = $"{message} {exception}";
            WriteToTrace(fullMessage, LogLevel.Info);
            AppendException(exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            WriteToTrace(fullMessage, LogLevel.Info);
        }

        public void Warn(string message)
        {
            WriteToTrace(message, LogLevel.Warn);
        }

        public void Warn(string message, Exception exception)
        {
            var fullMessage = $"{message} {exception}";
            WriteToTrace(fullMessage, LogLevel.Warn);
            AppendException(exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            WriteToTrace(fullMessage, LogLevel.Warn);
        }

        public void Error(string message)
        {
            WriteToTrace(message, LogLevel.Error);
        }

        public void Error(string message, Exception exception)
        {
            var fullMessage = $"{message} {exception}";
            WriteToTrace(fullMessage, LogLevel.Error);
            AppendException(exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            WriteToTrace(fullMessage, LogLevel.Error);
        }

        public void Fatal(string message)
        {
            WriteToTrace(message, LogLevel.Fatal);
        }

        public void Fatal(string message, Exception exception)
        {
            var fullMessage = $"{message} {exception}";
            WriteToTrace(fullMessage, LogLevel.Fatal);
            AppendException(exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            WriteToTrace(fullMessage, LogLevel.Fatal);
        }

        void WriteToTrace(string message, LogLevel messageSeverity)
        {
            if (level <= messageSeverity)
            {
                Trace.WriteLine(message);
                contextGetter()?.Logs.Enqueue(new ScenarioContext.LogItem
                {
                    Level = messageSeverity,
                    Message = message
                });
            }
        }

        LogLevel level;
        Func<ScenarioContext> contextGetter;
    }
}