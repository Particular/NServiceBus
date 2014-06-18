namespace NServiceBus.Logging
{
    using System;

    /// <summary>
    /// Responsible for the creation of <see cref="ILog"/> instances and used as an extension point to redirect log event to an external library.
    /// </summary>
    /// <remarks>
    /// The default logging will be to the console and a rolling log file.
    /// </remarks>
    public static class LogManager
    {
        static LogManager()
        {
            var defaultLogLevel = LogLevelReader.GetDefaultLogLevel();
            loggerFactory = new DefaultLoggerFactory(defaultLogLevel, null);
        }

        static ILoggerFactory loggerFactory; 
        internal static bool HasConfigBeenInitialised;

        /// <summary>
        /// An instance of <see cref="ILoggerFactory"/> that will be used to construct <see cref="ILog"/>s for static fields.
        /// </summary>
        /// <remarks>
        /// Replace this instance at application statup to redirect log event to your custom logging library.
        /// </remarks>
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

        /// <summary>
        /// Sets the <see cref="LoggerFactory"/> to be an instance of the internal default logger.
        /// </summary>
        /// <remarks>If <see cref="LoggerFactory"/> is already defined calling this method will replace it.</remarks>
        /// <param name="level">The minimum <see cref="System.LogLevel"/> threshold to use.</param>
        /// <param name="loggingDirectory">The target directory to log to.</param>
        public static void ConfigureDefaults(LogLevel level = LogLevel.Info, string loggingDirectory = null)
        {
            level = LogLevelReader.GetDefaultLogLevel(level);
            LoggerFactory = new DefaultLoggerFactory(level, loggingDirectory);
        }

        //TODO: perhaps add method for null logging

        /// <summary>
        /// Construct a <see cref="ILog"/> using <typeparamref name="T"/> as the name.
        /// </summary>
        public static ILog GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        /// <summary>
        /// Construct a <see cref="ILog"/> using <paramref name="type"/> as the name.
        /// </summary>
        public static ILog GetLogger(Type type)
        {
            return loggerFactory.GetLogger(type);
        }

        /// <summary>
        /// Construct a <see cref="ILog"/> for <paramref name="name"/>.
        /// </summary>
        public static ILog GetLogger(string name)
        {
            return loggerFactory.GetLogger(name);
        }
    }
}