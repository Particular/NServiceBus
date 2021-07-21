namespace NServiceBus.Testing
{
    using System;
    using System.IO;
    using Logging;

    class DefaultTestingLoggerFactory : ILoggerFactory
    {
        public DefaultTestingLoggerFactory(LogLevel filterLevel, TextWriter textWriter)
        {
            this.filterLevel = filterLevel;
            textWriterLogger = new TextWriterLogger(textWriter);
            isDebugEnabled = filterLevel <= LogLevel.Debug;
            isInfoEnabled = filterLevel <= LogLevel.Info;
            isWarnEnabled = filterLevel <= LogLevel.Warn;
            isErrorEnabled = filterLevel <= LogLevel.Error;
            isFatalEnabled = filterLevel <= LogLevel.Fatal;
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
#pragma warning disable PS0023 // Use DateTime.UtcNow or DateTimeOffset.UtcNow - For logging
            var datePart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
#pragma warning restore PS0023 // Use DateTime.UtcNow or DateTimeOffset.UtcNow
            var paddedLevel = messageLevel.ToString().ToUpper().PadRight(5);
            var fullMessage = $"{datePart} {paddedLevel} {name} {message}";
            lock (locker)
            {
                textWriterLogger.Write(fullMessage);
            }
        }

        LogLevel filterLevel;
        bool isDebugEnabled;
        bool isErrorEnabled;
        bool isFatalEnabled;
        bool isInfoEnabled;
        bool isWarnEnabled;

        object locker = new object();
        TextWriterLogger textWriterLogger;
    }
}