using System;
using System.Reflection;
using NServiceBus.Logging.Internal;

namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    /// <summary>
    /// 
    /// </summary>
    public class Configurator
    {

        private static readonly Type TargetType = Type.GetType("NLog.Targets.Target, NLog");
        private static readonly Type LogLevelType = Type.GetType("NLog.LogLevel, NLog");
        //private static readonly Type PatternLayoutType = Type.GetType("log4net.Layout.PatternLayout, log4net");
        private static readonly Type SimpleConfiguratorType = Type.GetType("NLog.Config.SimpleConfigurator, NLog");
        //private static readonly Type IAppenderType = Type.GetType("log4net.Appender.IAppender, log4net");

        public static void Basic(object target, string level = null)
        {
            EnsureNLogExists();

            if (!TargetType.IsInstanceOfType(target))
                throw new ArgumentException("The object provided must inherit from NLog.Targets.Target.");

            Basic();

            if (level != null)
                SimpleConfiguratorType.InvokeStaticMethod("ConfigureForTargetLogging", new[] { TargetType, LogLevelType }, new[] { target, LogLevelType.GetStaticField(level) });
            else
                SimpleConfiguratorType.InvokeStaticMethod("ConfigureForTargetLogging", new[] { TargetType }, new[] { target });

            //if (appenderSkeleton.GetProperty("Layout") == null)
            //    appenderSkeleton.SetProperty("Layout", Activator.CreateInstance(PatternLayoutType, "%d [%t] %-5p %c [%x] <%X{auth}> - %m%n"));

            //if (threshold != null)
            //    AppenderFactory.SetThreshold(appenderSkeleton, threshold);

            //if (AppenderFactory.GetThreshold(appenderSkeleton) == null)
            //    AppenderFactory.SetThreshold(appenderSkeleton, "Info");

            //appenderSkeleton.InvokeMethod("ActivateOptions");

            //BasicConfiguratorType
            //    .GetMethod("Configure", BindingFlags.Public | BindingFlags.Static, null, new[] {IAppenderType}, null)
            //    .Invoke(null, new[] {appenderSkeleton});

        }

        /// <summary>
        /// Configure NServiceBus to use Log4Net without setting a specific appender.
        /// </summary>
        public static void Basic()
        {
            EnsureNLogExists();

            LogManager.LoggerFactory = new LoggerFactory();
        }

        private static void EnsureNLogExists()
        {
            if (SimpleConfiguratorType == null)
                throw new LoggingLibraryException("NLog could not be loaded. Make sure that the NLog assembly is located in the executable directory.");
        }
    }
}