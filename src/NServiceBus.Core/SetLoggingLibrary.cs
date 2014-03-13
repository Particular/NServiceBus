namespace NServiceBus
{
    using System;
    using System.Linq;
    using Logging;
    using Logging.Loggers.NLogAdapter;

    /// <summary>
    /// Class containing extension method to allow users to use Log4Net for logging
    /// </summary>
    public static class SetLoggingLibrary
    {
        /// <summary>
        /// Use Log4Net for logging with the Console Appender at the level of All.
        /// </summary>
        public static Configure Log4Net(this Configure config)
        {
            var appender = Logging.Loggers.Log4NetAdapter.Log4NetAppenderFactory.CreateConsoleAppender("All");

            return config.Log4Net(appender);
        }

        /// <summary>
        /// Use Log4Net for logging with your own appender type, initializing it as necessary.
        /// Will call 'ActivateOptions()' on the appender for you.
        /// If you don't specify a threshold, will default to Level.Debug.
        /// If you don't specify layout, uses this as a default: %d [%t] %-5p %c [%x] &lt;%X{auth}&gt; - %m%n
        /// </summary>
        public static Configure Log4Net<TAppender>(this Configure config, Action<TAppender> initializeAppender) where TAppender : new()
        {
            var appender = new TAppender();
            initializeAppender(appender);

            return config.Log4Net(appender);
        }

        /// <summary>
        /// Use Log4Net for logging passing in a pre-configured appender.
        /// Will call 'ActivateOptions()' on the appender for you.
        /// If you don't specify a threshold, will default to Level.Debug.
        /// If you don't specify layout, uses this as a default: %d [%t] %-5p %c [%x] &lt;%X{auth}&gt; - %m%n
        /// </summary>
        public static Configure Log4Net(this Configure config, object appenderSkeleton)
        {
            var threshold = GetThresholdFromConfigSection();

            Logging.Loggers.Log4NetAdapter.Log4NetConfigurator.Configure(appenderSkeleton, threshold);

            return config;
        }

        static string GetThresholdFromConfigSection()
        {
            var cfg = Configure.GetConfigSection<Config.Logging>();
            if (cfg != null)
            {
                if (cfg.Threshold == String.Empty)
                {
                    return cfg.Threshold;
                }
            }
            return null;
        }

        /// <summary>
        /// Configure NServiceBus to use Log4Net without setting a specific appender.
        /// </summary>
        public static void Log4Net()
        {
            Logging.Loggers.Log4NetAdapter.Log4NetConfigurator.Configure();
        }



        /// <summary>
        /// Configure NServiceBus to use Log4Net and specify your own configuration.
        /// Use 'log4net.Config.XmlConfigurator.Configure' as the parameter to get the configuration from the app.config.
        /// </summary>
        public static void Log4Net(Action config)
        {
            Log4Net();

            config();
        }

        public static Configure NLog(this Configure config, params object[] targetsForNServiceBusToLogTo)
        {

            if (targetsForNServiceBusToLogTo == null)
            {
                throw new ArgumentNullException("targetsForNServiceBusToLogTo");
            }
            if (targetsForNServiceBusToLogTo.Length == 0)
            {
                throw new ArgumentException("Must not be empty.", "targetsForNServiceBusToLogTo");
            }
            if (targetsForNServiceBusToLogTo.Any(x => x == null))
            {
                throw new ArgumentNullException("targetsForNServiceBusToLogTo", "Must not contain null values.");
            }

            var threshold = GetThresholdFromConfigSection();
            NLogConfigurator.Configure(targetsForNServiceBusToLogTo, threshold);

            return config;
        }

        public static void NLog()
        {
            NLogConfigurator.Configure();
        }

        public static void Custom(ILoggerFactory loggerFactory)
        {
            LogManager.LoggerFactory = loggerFactory;
        }
    }
}