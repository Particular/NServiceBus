namespace NServiceBus
{
    using System;
    using Logging;

    /// <summary>
    /// Class containing extension method to allow users to use Log4Net for logging
    /// </summary>
    public static class SetLoggingLibrary
    {
        /// <summary>
        /// Configure NServiceBus to use Log4Net
        /// </summary>
        public static void Log4Net()
        {
            LogManager.LoggerFactory = new Logging.Loggers.Log4NetAdapter.LoggerFactory();
        }

        public static void NLog()
        {
            LogManager.LoggerFactory = new Logging.Loggers.NLogAdapter.LoggerFactory();
        }

        public static void Custom(ILoggerFactory loggerFactory)
        {
            LogManager.LoggerFactory = loggerFactory;
        }

        /// <summary>
        /// Use Log4Net for logging with your own appender type, initializing it as necessary.
        /// Will call 'ActivateOptions()' on the appender for you.
        /// If you don't specify a threshold, will default to Level.Debug.
        /// If you don't specify layout, uses this as a default: %d [%t] %-5p %c [%x] &lt;%X{auth}&gt; - %m%n
        /// </summary>
        [ObsoleteEx(Message = "Use Log4Net() instead and configure your own appenders", RemoveInVersion = "4.0.0")]
        public static Configure Log4Net<TAppender>(this Configure config, Action<TAppender> initializeAppender) where TAppender : new()
        {
            throw new LoggingLibraryException("This method is not supported anymore. Use Log4Net() instead and configure your own appenders");
        }

        /// <summary>
        /// Use Log4Net for logging passing in a pre-configured appender.
        /// Will call 'ActivateOptions()' on the appender for you.
        /// If you don't specify a threshold, will default to Level.Debug.
        /// If you don't specify layout, uses this as a default: %d [%t] %-5p %c [%x] &lt;%X{auth}&gt; - %m%n
        /// </summary>
        [ObsoleteEx(Message = "Use Log4Net() instead and configure your own appenders", RemoveInVersion="4.0.0")]
        public static Configure Log4Net(this Configure config, object appenderSkeleton)
        {
            throw new LoggingLibraryException("This method is not supported anymore. Use Log4Net() instead and configure your own appenders");
        }
        
        /// <summary>
        /// Configure NServiceBus to use Log4Net and specify your own configuration.
        /// Use 'log4net.Config.XmlConfigurator.Configure' as the parameter to get the configuration from the app.config.
        /// </summary>
        [ObsoleteEx(Message = "Use Log4Net() instead and configure your own appenders", RemoveInVersion = "4.0.0")]
        public static void Log4Net(Action config)
        {
            throw new LoggingLibraryException("This method is not supported anymore. Use Log4Net() instead and configure your own appenders");
        }
    }
}