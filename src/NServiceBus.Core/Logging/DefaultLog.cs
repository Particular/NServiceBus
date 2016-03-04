namespace NServiceBus
{
    using System;
    using Logging;

    class NamedLogger : ILog
    {
        public NamedLogger(string name, DefaultLoggerFactory defaultLoggerFactory)
        {
            this.name = name;
            this.defaultLoggerFactory = defaultLoggerFactory;
        }

        public bool IsDebugEnabled { get; internal set; }
        public bool IsInfoEnabled { get; internal set; }
        public bool IsWarnEnabled { get; internal set; }
        public bool IsErrorEnabled { get; internal set; }
        public bool IsFatalEnabled { get; internal set; }

        public void Debug(string message)
        {
            defaultLoggerFactory.Write(name, LogLevel.Debug, message);
        }

        public void Debug(string message, Exception exception)
        {
            defaultLoggerFactory.Write(name, LogLevel.Debug, message + Environment.NewLine + exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            defaultLoggerFactory.Write(name, LogLevel.Debug, string.Format(format, args));
        }

        public void Info(string message)
        {
            defaultLoggerFactory.Write(name, LogLevel.Info, message);
        }

        public void Info(string message, Exception exception)
        {
            defaultLoggerFactory.Write(name, LogLevel.Info, message + Environment.NewLine + exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            defaultLoggerFactory.Write(name, LogLevel.Info, string.Format(format, args));
        }

        public void Warn(string message)
        {
            defaultLoggerFactory.Write(name, LogLevel.Warn, message);
        }

        public void Warn(string message, Exception exception)
        {
            defaultLoggerFactory.Write(name, LogLevel.Warn, message + Environment.NewLine + exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            defaultLoggerFactory.Write(name, LogLevel.Warn, string.Format(format, args));
        }

        public void Error(string message)
        {
            defaultLoggerFactory.Write(name, LogLevel.Error, message);
        }

        public void Error(string message, Exception exception)
        {
            defaultLoggerFactory.Write(name, LogLevel.Error, message + Environment.NewLine + exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            defaultLoggerFactory.Write(name, LogLevel.Error, string.Format(format, args));
        }

        public void Fatal(string message)
        {
            defaultLoggerFactory.Write(name, LogLevel.Fatal, message);
        }

        public void Fatal(string message, Exception exception)
        {
            defaultLoggerFactory.Write(name, LogLevel.Error, message + Environment.NewLine + exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            defaultLoggerFactory.Write(name, LogLevel.Fatal, string.Format(format, args));
        }

        DefaultLoggerFactory defaultLoggerFactory;
        string name;
    }
}