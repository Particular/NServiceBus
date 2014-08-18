namespace NServiceBus.Logging
{
    using System;
    using System.Diagnostics;

    class DefaultLoggerFactory : ILoggerFactory
    {
        LogLevel filterLevel;
        bool isDebugEnabled;
        bool isInfoEnabled;
        bool isWarnEnabled;
        bool isErrorEnabled;
        bool isFatalEnabled;
        RollingLogger rollingLogger;

        object locker = new object();
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
                IsFatalEnabled = isFatalEnabled,
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
            var fullMessage = string.Format("{0} {1} {2} {3}", datePart, paddedLevel, name, message);
            lock (locker)
            {
                rollingLogger.Write(fullMessage);
                ColoredConsoleLogger.Write(fullMessage, messageLevel);
                Trace.WriteLine(fullMessage);
            }
        }
    }
}