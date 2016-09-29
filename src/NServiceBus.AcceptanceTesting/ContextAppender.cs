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
        public ContextAppender(LogLevel level, Func<ScenarioContext> context)
        {
            this.level = level;
            this.context = context;
        }

        public bool IsDebugEnabled => level <= LogLevel.Debug;
        public bool IsInfoEnabled => level <= LogLevel.Info;
        public bool IsWarnEnabled => level <= LogLevel.Warn;
        public bool IsErrorEnabled => level <= LogLevel.Error;
        public bool IsFatalEnabled => level <= LogLevel.Fatal;


        public void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public void Debug(string message, Exception exception)
        {
            Log(message, LogLevel.Debug);
        }

        public void DebugFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Log(fullMessage, LogLevel.Debug);
        }

        public void Info(string message)
        {
            Log(message, LogLevel.Info);
        }


        public void Info(string message, Exception exception)
        {
            var fullMessage = $"{message} {exception}";
            Log(fullMessage, LogLevel.Info);
        }

        public void InfoFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Log(fullMessage, LogLevel.Info);
        }

        public void Warn(string message)
        {
            Log(message, LogLevel.Warn);
        }

        public void Warn(string message, Exception exception)
        {
            var fullMessage = $"{message} {exception}";
            Log(fullMessage, LogLevel.Warn);
        }

        public void WarnFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Log(fullMessage, LogLevel.Warn);
        }

        public void Error(string message)
        {
            Log(message, LogLevel.Error);
        }

        public void Error(string message, Exception exception)
        {
            var fullMessage = $"{message} {exception}";
            Log(fullMessage, LogLevel.Error);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Log(fullMessage, LogLevel.Error);
        }

        public void Fatal(string message)
        {
            Log(message, LogLevel.Fatal);
        }

        public void Fatal(string message, Exception exception)
        {
            var fullMessage = $"{message} {exception}";
            Log(fullMessage, LogLevel.Fatal);
        }

        public void FatalFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Log(fullMessage, LogLevel.Fatal);
        }

        void Log(string message, LogLevel messageSeverity)
        {
            if (level <= messageSeverity)
            {
                Trace.WriteLine(message);
                context().Logs.Enqueue(new ScenarioContext.LogItem
                {
                    Level = messageSeverity,
                    Message = message
                });
            }
        }

        LogLevel level;
        Func<ScenarioContext> context;
    }
}