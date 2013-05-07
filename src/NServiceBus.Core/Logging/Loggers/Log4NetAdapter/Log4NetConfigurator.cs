namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    using System;
    using Internal;

    /// <summary>
    /// 
    /// </summary>
    public class Log4NetConfigurator
    {
        private static readonly Type AppenderSkeletonType = Type.GetType("log4net.Appender.AppenderSkeleton, log4net");
        private static readonly Type PatternLayoutType = Type.GetType("log4net.Layout.PatternLayout, log4net");
        private static readonly Type BasicConfiguratorType = Type.GetType("log4net.Config.BasicConfigurator, log4net");

        public static bool Log4NetExists
        {
            get { return Type.GetType("log4net.LogManager, log4net") != null; }
        }

        public static void Configure(dynamic appenderForNServiceBusToLogTo, string thresholdForNServiceBusToLogWith = null)
        {
           if (appenderForNServiceBusToLogTo == null)
            {
                throw new ArgumentNullException("appenderForNServiceBusToLogTo");
            }
            EnsureLog4NetExists();

            if (!AppenderSkeletonType.IsInstanceOfType(appenderForNServiceBusToLogTo))
                throw new ArgumentException("The object provided must inherit from log4net.Appender.AppenderSkeleton.");

            Configure();

            if (appenderForNServiceBusToLogTo.Layout == null)
                appenderForNServiceBusToLogTo.Layout = (dynamic)Activator.CreateInstance(PatternLayoutType, "%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");

            if (thresholdForNServiceBusToLogWith != null)
                appenderForNServiceBusToLogTo.Threshold = Log4NetAppenderFactory.ConvertToLogLevel(thresholdForNServiceBusToLogWith);

            if (appenderForNServiceBusToLogTo.Threshold == null)
                appenderForNServiceBusToLogTo.Threshold = Log4NetAppenderFactory.ConvertToLogLevel("Info");

            appenderForNServiceBusToLogTo.ActivateOptions();

            BasicConfiguratorType.InvokeStaticMethod("Configure", (object)appenderForNServiceBusToLogTo);
        }

        /// <summary>
        /// Configure NServiceBus to use Log4Net without setting a specific appender.
        /// </summary>
        public static void Configure()
        {
            EnsureLog4NetExists();

            LogManager.LoggerFactory = new Log4NetLoggerFactory();
        }

        private static void EnsureLog4NetExists()
        {
            if (!Log4NetExists)
                throw new LoggingLibraryException("Log4net could not be loaded. Make sure that the log4net assembly is located in the executable directory.");
        }
    }
}