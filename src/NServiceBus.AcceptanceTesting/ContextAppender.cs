namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Remoting.Messaging;
    using Logging;

    // This class is written under the assumption that acceptance tests are executed sequentially.
    class ContextAppender : ILog
    {
        public bool IsDebugEnabled => ((ScenarioContext)CallContext.LogicalGetData("ScenarioContext")).LogLevel <= LogLevel.Debug;
        public bool IsInfoEnabled => ((ScenarioContext)CallContext.LogicalGetData("ScenarioContext")).LogLevel <= LogLevel.Info;
        public bool IsWarnEnabled => ((ScenarioContext)CallContext.LogicalGetData("ScenarioContext")).LogLevel <= LogLevel.Warn;
        public bool IsErrorEnabled => ((ScenarioContext)CallContext.LogicalGetData("ScenarioContext")).LogLevel <= LogLevel.Error;
        public bool IsFatalEnabled => ((ScenarioContext)CallContext.LogicalGetData("ScenarioContext")).LogLevel <= LogLevel.Fatal;


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

        static void Log(string message, LogLevel messageSeverity)
        {
            var context = (ScenarioContext) CallContext.LogicalGetData("ScenarioContext");

            if (context.LogLevel > messageSeverity)
            {
                return;
            }

            Trace.WriteLine(message);
            context.Logs.Enqueue(new ScenarioContext.LogItem
            {
                Level = messageSeverity,
                Message = message
            });
        }
    }
}