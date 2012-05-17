using System;
using System.Reflection;
using NServiceBus.Logging.Internal;

namespace NServiceBus.Logging.Log4Net
{
    /// <summary>
    /// 
    /// </summary>
    public class Configurator
    {

        private static readonly Type AppenderSkeletonType = Type.GetType("log4net.Appender.AppenderSkeleton, log4net");
        private static readonly Type PatternLayoutType = Type.GetType("log4net.Layout.PatternLayout, log4net");
        private static readonly Type BasicConfiguratorType = Type.GetType("log4net.Config.BasicConfigurator, log4net");
        private static readonly Type IAppenderType = Type.GetType("log4net.Appender.IAppender, log4net");

        public static void Basic(object appenderSkeleton, string threshold = null)
        {
            EnsureLog4NetExists();

            if (!AppenderSkeletonType.IsInstanceOfType(appenderSkeleton))
                throw new ArgumentException("The object provided must inherit from log4net.Appender.AppenderSkeleton.");

            Basic();

            if (appenderSkeleton.GetProperty("Layout") == null)
                appenderSkeleton.SetProperty("Layout", Activator.CreateInstance(PatternLayoutType, "%d [%t] %-5p %c [%x] <%X{auth}> - %m%n"));

            if (threshold != null)
                AppenderFactory.SetThreshold(appenderSkeleton, threshold);

            if (AppenderFactory.GetThreshold(appenderSkeleton) == null)
                AppenderFactory.SetThreshold(appenderSkeleton, "Info");

            appenderSkeleton.InvokeMethod("ActivateOptions");

            BasicConfiguratorType
                .GetMethod("Configure", BindingFlags.Public | BindingFlags.Static, null, new[] {IAppenderType}, null)
                .Invoke(null, new[] {appenderSkeleton});

        }

        /// <summary>
        /// Configure NServiceBus to use Log4Net without setting a specific appender.
        /// </summary>
        public static void Basic()
        {
            EnsureLog4NetExists();

            LogManager.LoggerFactory = new LoggerFactory();
        }

        private static void EnsureLog4NetExists()
        {
            if (BasicConfiguratorType == null)
                throw new LoggingLibraryException("Log4net could not be loaded. Make sure that the log4net assembly is located in the executable directory.");
        }
    }
}