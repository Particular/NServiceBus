namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using NUnit.Framework;

    public class TransportTestLoggerFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            return new TransportTestLogger(name, LogItems);
        }

        public List<LogItem> LogItems { get; } = new List<LogItem>();

        public class LogItem
        {
            public LogLevel Level;
            public string Message;
        }

        class TransportTestLogger : ILog
        {
            public TransportTestLogger(string name, List<LogItem> logItems)
            {
                this.name = name;
                this.logItems = logItems;
            }

            public bool IsDebugEnabled { get; } = true;
            public bool IsInfoEnabled { get; } = true;
            public bool IsWarnEnabled { get; } = true;
            public bool IsErrorEnabled { get; } = true;
            public bool IsFatalEnabled { get; } = true;

            public void Debug(string message)
            {
                Log(LogLevel.Debug, message);
            }

            public void Debug(string message, Exception exception)
            {
                Log(LogLevel.Debug, $"{message} {exception}");
            }

            public void DebugFormat(string format, params object[] args)
            {
                Log(LogLevel.Debug, string.Format(format, args));
            }

            public void Info(string message)
            {
                Log(LogLevel.Info, message);
            }

            public void Info(string message, Exception exception)
            {
                Log(LogLevel.Info, $"{message} {exception}");
            }

            public void InfoFormat(string format, params object[] args)
            {
                Log(LogLevel.Info, string.Format(format, args));
            }

            public void Warn(string message)
            {
                Log(LogLevel.Warn, message);
            }

            public void Warn(string message, Exception exception)
            {
                Log(LogLevel.Warn, $"{message} {exception}");
            }

            public void WarnFormat(string format, params object[] args)
            {
                Log(LogLevel.Warn, string.Format(format, args));
            }

            public void Error(string message)
            {
                Log(LogLevel.Error, message);
            }

            public void Error(string message, Exception exception)
            {
                Log(LogLevel.Error, $"{message} {exception}");
            }

            public void ErrorFormat(string format, params object[] args)
            {
                Log(LogLevel.Error, string.Format(format, args));
            }

            public void Fatal(string message)
            {
                Log(LogLevel.Fatal, message);
            }

            public void Fatal(string message, Exception exception)
            {
                Log(LogLevel.Fatal, $"{message} {exception}");
            }

            public void FatalFormat(string format, params object[] args)
            {
                Log(LogLevel.Fatal, string.Format(format, args));
            }

            void Log(LogLevel level, string message)
            {
                logItems.Add(new LogItem
                {
                    Level = level,
                    Message = message
                });

                TestContext.WriteLine($"{DateTime.Now:T} {level} {name}: {message}");
            }

            string name;
            List<LogItem> logItems;
        }
    }
}