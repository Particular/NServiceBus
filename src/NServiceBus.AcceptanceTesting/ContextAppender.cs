namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Diagnostics;
    using Logging;

    class ContextAppender : ILog
    {
        public ContextAppender(string logger)
        {
            this.logger = logger;
        }

        public bool IsDebugEnabled => ScenarioContext.Current.LogLevel <= LogLevel.Debug;
        public bool IsInfoEnabled => ScenarioContext.Current.LogLevel <= LogLevel.Info;
        public bool IsWarnEnabled => ScenarioContext.Current.LogLevel <= LogLevel.Warn;
        public bool IsErrorEnabled => ScenarioContext.Current.LogLevel <= LogLevel.Error;
        public bool IsFatalEnabled => ScenarioContext.Current.LogLevel <= LogLevel.Fatal;

        public void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public void Debug(string message, Exception exception)
        {
            var fullMessage = $"{message} {exception}";
            Log(fullMessage, LogLevel.Debug);
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
            var context = ScenarioContext.Current;
            if (context == null)
            {
                // avoid NRE in case something logs outside of a test scenario
                Console.WriteLine(message);
                return;
            }

            if (context.LogLevel > messageSeverity)
                return;

            Trace.WriteLine(message);
            context.Logs.Enqueue(new ScenarioContext.LogItem
            {
                Endpoint = ScenarioContext.CurrentEndpoint,
                LoggerName = logger,
                Level = messageSeverity,
                Message = message
            });
        }

        string logger;
    }
}