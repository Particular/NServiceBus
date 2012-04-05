using System;
using System.Collections.Specialized;
using NServiceBus.Logging;
using log4net.Appender;
using log4net.Core;
using System.Reflection;

namespace NServiceBus
{
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
            return config.Log4Net<ConsoleAppender>(ca => ca.Threshold = Level.All);
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
            var appender = appenderSkeleton as AppenderSkeleton;
            if (appender == null)
                throw new ArgumentException("The object provided must inherit from log4net.Appender.AppenderSkeleton.");

            Log4Net();

            if (appender.Layout == null)
                appender.Layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");

            var cfg = Configure.GetConfigSection<Config.Logging>();
            if (cfg != null)
            {
                foreach(var f in typeof(Level).GetFields(BindingFlags.Public | BindingFlags.Static))
                    if (string.Compare(cfg.Threshold, f.Name, true) == 0)
                    {
                        var val = f.GetValue(null);
                        appender.Threshold = val as Level;
                        break;
                    }
            }

            if (appender.Threshold == null)
                appender.Threshold = Level.Info;

            appender.ActivateOptions();

            log4net.Config.BasicConfigurator.Configure(appender);

            return config;
        }

        /// <summary>
        /// Configure NServiceBus to use Log4Net without setting a specific appender.
        /// </summary>
        public static void Log4Net()
        {
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

    }
}