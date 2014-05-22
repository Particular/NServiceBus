namespace NServiceBus.NLog
{
    using Logging;

    /// <summary>
    /// Configure NServiceBus logging messages to use NLog.
    /// </summary>
    public static class NLogConfigurator
    {

        /// <summary>
        /// Configure NServiceBus logging messages to use NLog. This method should be called before <see cref="NServiceBus.Configure.With()"/>.
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
            var log = loggerFactory.GetLogger(typeof(NLogConfigurator));
            log.Warn("You have called NLogConfigurator.Configure() after NServiceBus.Configure.With() has been called. To capture messages and errors that occur during configuration you should call NLogConfigurator.Configure() before NServiceBus.Configure.With().");
        }
    }
}