using System;
using System.Collections.Specialized;
using NServiceBus.Logging;
using System.Reflection;

namespace NServiceBus
{
    /// <summary>
    /// Class containing extension method to allow users to use Log4Net for logging
    /// </summary>
    public static class SetLoggingLibrary
    {
        private static readonly Type AppenderSkeletonType = Type.GetType("log4net.Appender.AppenderSkeleton, log4net");
        private static readonly Type ConsoleAppenderType = Type.GetType("log4net.Appender.ConsoleAppender, log4net");
        private static readonly Type LevelType = Type.GetType("log4net.Core.Level, log4net");
        private static readonly Type PatternLayoutType = Type.GetType("log4net.Layout.PatternLayout, log4net");
        private static readonly Type BasicConfiguratorType = Type.GetType("log4net.Config.BasicConfigurator, log4net");
        private static readonly Type IAppenderType = Type.GetType("log4net.Appender.IAppender, log4net");

        /// <summary>
        /// Use Log4Net for logging with the Console Appender at the level of All.
        /// </summary>
        public static Configure Log4Net(this Configure config)
        {
            EnsureLog4NetExists();

            var threshold = GetLevelThreshold("All");

            var consoleAppender = Activator.CreateInstance(ConsoleAppenderType);
            ConsoleAppenderType.GetProperty("Threshold").SetValue(consoleAppender, threshold, null);

            return config.Log4Net(consoleAppender);
        }

        /// <summary>
        /// Use Log4Net for logging with your own appender type, initializing it as necessary.
        /// Will call 'ActivateOptions()' on the appender for you.
        /// If you don't specify a threshold, will default to Level.Debug.
        /// If you don't specify layout, uses this as a default: %d [%t] %-5p %c [%x] &lt;%X{auth}&gt; - %m%n
        /// </summary>
        public static Configure Log4Net<TAppender>(this Configure config, Action<TAppender> initializeAppender) where TAppender : new()
        {
            EnsureLog4NetExists();
            
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
            EnsureLog4NetExists();

            if (!AppenderSkeletonType.IsInstanceOfType(appenderSkeleton))
                throw new ArgumentException("The object provided must inherit from log4net.Appender.AppenderSkeleton.");

            Log4Net();

            var layoutProperty = AppenderSkeletonType.GetProperty("Layout");
            var thresholdProperty = AppenderSkeletonType.GetProperty("Threshold");

            if (layoutProperty.GetValue(appenderSkeleton, null) == null)
                layoutProperty.SetValue(appenderSkeleton, Activator.CreateInstance(PatternLayoutType, "%d [%t] %-5p %c [%x] <%X{auth}> - %m%n"), null);

            var cfg = Configure.GetConfigSection<Config.Logging>();
            if (cfg != null)
            {
                foreach (var f in LevelType.GetFields(BindingFlags.Public | BindingFlags.Static))
                    if (string.Compare(cfg.Threshold, f.Name, true) == 0)
                    {
                        var val = f.GetValue(null);
                        thresholdProperty.SetValue(appenderSkeleton, val, null);
                        break;
                    }
            }

            if (thresholdProperty.GetValue(appenderSkeleton, null) == null)
                thresholdProperty.SetValue(appenderSkeleton, GetLevelThreshold("Info"), null);

            AppenderSkeletonType
              .GetMethod("ActivateOptions", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance)
              .Invoke(appenderSkeleton, null);

            BasicConfiguratorType
              .GetMethod("Configure", BindingFlags.Public | BindingFlags.Static, null, new[] { IAppenderType }, null)
              .Invoke(null, new[] { appenderSkeleton });

            return config;
        }

        /// <summary>
        /// Configure NServiceBus to use Log4Net without setting a specific appender.
        /// </summary>
        public static void Log4Net()
        {
            EnsureLog4NetExists();

            var props = new NameValueCollection();
            LogManager.LoggerFactory = new Logging.Log4Net.LoggerFactory();
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

        private static object GetLevelThreshold(string name)
        {
            return LevelType.GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }
        
        private static void EnsureLog4NetExists()
        {
            if (BasicConfiguratorType == null)
                throw new LoggingLibraryException("Log4net could not be loaded. Make sure that the log4net assembly is located in the executable directory.");
        }
    }
}