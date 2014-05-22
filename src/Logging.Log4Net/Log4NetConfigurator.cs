namespace NServiceBus.Log4Net
{
    using Logging;

    /// <summary>
    /// Configure NServiceBus logging messages to use Log4Net.
    /// </summary>
    public static class Log4NetConfigurator
    {

        /// <summary>
        /// Configure NServiceBus logging messages to use Log4Net. This method should be called before <see cref="NServiceBus.Configure.With()"/>.
        /// </summary>
        public static void Configure()
        {
            var loggerFactory = new LoggerFactory();
            LogManager.LoggerFactory = loggerFactory;
            LogAlreadyConfiguredWarning(loggerFactory);
        }

        static void LogAlreadyConfiguredWarning(LoggerFactory loggerFactory)
        {
            if (NServiceBus.Configure.Instance == null)
            {
                return;
            }
            var log = loggerFactory.GetLogger(typeof(Log4NetConfigurator));
            log.Warn("You have called Log4NetConfigurator.Configure() after NServiceBus.Configure.With() has been called. To capture messages and errors that occur during configuration you should call Log4NetConfigurator.Configure() before NServiceBus.Configure.With().");
        }
    }
}