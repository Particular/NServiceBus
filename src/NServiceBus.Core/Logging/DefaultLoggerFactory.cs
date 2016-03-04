namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using Logging;

    class DefaultLoggerFactory : ILoggerFactory
    {
        public DefaultLoggerFactory(LogLevel filterLevel, string loggingDirectory)
        {
            this.filterLevel = filterLevel;
            rollingLogger = new RollingLogger(loggingDirectory);
            isDebugEnabled = LogLevel.Debug >= filterLevel;
            isInfoEnabled = LogLevel.Info >= filterLevel;
            isWarnEnabled = LogLevel.Warn >= filterLevel;
            isErrorEnabled = LogLevel.Error >= filterLevel;
            isFatalEnabled = LogLevel.Fatal >= filterLevel;
        }

        public ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            return new NamedLogger(name, this)
            {
                IsDebugEnabled = isDebugEnabled,
                IsInfoEnabled = isInfoEnabled,
                IsWarnEnabled = isWarnEnabled,
                IsErrorEnabled = isErrorEnabled,
                IsFatalEnabled = isFatalEnabled
            };
        }

        public void Write(string name, LogLevel messageLevel, string message)
        {
            if (messageLevel < filterLevel)
            {
                return;
            }
            var datePart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var paddedLevel = messageLevel.ToString().ToUpper().PadRight(5);
            var fullMessage = $"{datePart} {paddedLevel} {name} {message}";
            lock (locker)
            {
                rollingLogger.Write(fullMessage);
                ColoredConsoleLogger.Write(fullMessage, messageLevel);
                Trace.WriteLine(fullMessage);
            }
        }

        LogLevel filterLevel;
        bool isDebugEnabled;
        bool isErrorEnabled;
        bool isFatalEnabled;
        bool isInfoEnabled;
        bool isWarnEnabled;

        object locker = new object();
        RollingLogger rollingLogger;
    }
}