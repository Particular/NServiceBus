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

        public static void Configure(object appenderSkeleton, string threshold = null)
        {
            EnsureLog4NetExists();

            if (!AppenderSkeletonType.IsInstanceOfType(appenderSkeleton))
                throw new ArgumentException("The object provided must inherit from log4net.Appender.AppenderSkeleton.");

            Configure();

            if (appenderSkeleton.GetProperty("Layout") == null)
                appenderSkeleton.SetProperty("Layout", Activator.CreateInstance(PatternLayoutType, "%d [%t] %-5p %c [%x] <%X{auth}> - %m%n"));

            if (threshold != null)
                Log4NetAppenderFactory.SetThreshold(appenderSkeleton, threshold);

            if (Log4NetAppenderFactory.GetThreshold(appenderSkeleton) == null)
                Log4NetAppenderFactory.SetThreshold(appenderSkeleton, "Info");

            appenderSkeleton.InvokeMethod("ActivateOptions");

            BasicConfiguratorType.InvokeStaticMethod("Configure", appenderSkeleton);
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