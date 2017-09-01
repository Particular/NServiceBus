namespace NServiceBus.TransportTests
{
    using System;
    using Logging;
    using NUnit.Framework;

    public class TransportTestLoggerFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return this.GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            return new TransportTestLogger(name);
        }

        class TransportTestLogger : ILog
        {
            string name;

            public TransportTestLogger(string name)
            {
                this.name = name;
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
                TestContext.WriteLine($"{DateTime.Now:T} {level} {name}: {message}");
            }
        }
    }
}