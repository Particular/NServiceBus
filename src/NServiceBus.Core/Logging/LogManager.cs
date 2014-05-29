namespace NServiceBus.Logging
{
    using System;

    public static class LogManager
    {

        static LogManager()
        {
            var defaultLogLevel = LogLevelReader.GetDefaultLogLevel();
            loggerFactory = new DefaultLoggerFactory(defaultLogLevel, null);
        }
        static ILoggerFactory loggerFactory;
        internal static bool HasConfigBeenInitialised;
        public static ILoggerFactory LoggerFactory
        {
            get { return loggerFactory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                loggerFactory = value;

                if (!HasConfigBeenInitialised)
                {
                    return;
                }
                var log = loggerFactory.GetLogger(typeof(LogManager));
                log.Warn("Logging has been configured after NServiceBus.Configure.With() has been called. To capture messages and errors that occur during configuration logging should be configured before before NServiceBus.Configure.With().");
            }
        }

        public static void ConfigureDefaults(LogLevel level = LogLevel.Info, string loggingDirectory = null)
        {
            level = LogLevelReader.GetDefaultLogLevel(level);
            LoggerFactory = new DefaultLoggerFactory(level, loggingDirectory);
        }

        //TODO: perhaps add method for null logging

        public static ILog GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        public static ILog GetLogger(Type type)
        {
            return loggerFactory.GetLogger(type);
        }

        public static ILog GetLogger(string name)
        {
            return loggerFactory.GetLogger(name);
        }
    }
}