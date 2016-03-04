namespace NServiceBus.Logging
{
    using System;

    /// <summary>
    /// Responsible for the creation of <see cref="ILog" /> instances and used as an extension point to redirect log event to
    /// an external library.
    /// </summary>
    /// <remarks>
    /// The default logging will be to the console and a rolling log file.
    /// </remarks>
    public static class LogManager
    {
        static LogManager()
        {
            Use<DefaultFactory>();
        }

        /// <summary>
        /// Used to inject an instance of <see cref="ILoggerFactory" /> into <see cref="LogManager" />.
        /// </summary>
        public static T Use<T>() where T : LoggingFactoryDefinition, new()
        {
            var loggingDefinition = new T();

            loggerFactory = new Lazy<ILoggerFactory>(loggingDefinition.GetLoggingFactory);

            return loggingDefinition;
        }

        /// <summary>
        /// An instance of <see cref="ILoggerFactory" /> that will be used to construct <see cref="ILog" />s for static fields.
        /// </summary>
        /// <remarks>
        /// Replace this instance at application statup to redirect log event to the custom logging library.
        /// </remarks>
        public static void UseFactory(ILoggerFactory loggerFactory)
        {
            Guard.AgainstNull(nameof(loggerFactory), loggerFactory);

            LogManager.loggerFactory = new Lazy<ILoggerFactory>(() => loggerFactory);
        }

        /// <summary>
        /// Construct a <see cref="ILog" /> using <typeparamref name="T" /> as the name.
        /// </summary>
        public static ILog GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        /// <summary>
        /// Construct a <see cref="ILog" /> using <paramref name="type" /> as the name.
        /// </summary>
        public static ILog GetLogger(Type type)
        {
            Guard.AgainstNull(nameof(type), type);
            return loggerFactory.Value.GetLogger(type);
        }

        /// <summary>
        /// Construct a <see cref="ILog" /> for <paramref name="name" />.
        /// </summary>
        public static ILog GetLogger(string name)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);
            return loggerFactory.Value.GetLogger(name);
        }

        static Lazy<ILoggerFactory> loggerFactory;
    }
}