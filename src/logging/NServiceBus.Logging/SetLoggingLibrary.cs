using System;
using System.Collections.Specialized;
using System.Configuration;
using log4net.Appender;
using log4net.Core;
using System.Globalization;
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
        public static Configure Log4Net<Appender>(this Configure config, Action<Appender> initializeAppender) where Appender : AppenderSkeleton, new()
        {
            var appender = new Appender();
            initializeAppender(appender);

            return config.Log4Net(appender);
        }

        /// <summary>
        /// Use Log4Net for logging passing in a pre-configured appender.
        /// Will call 'ActivateOptions()' on the appender for you.
        /// If you don't specify a threshold, will default to Level.Debug.
        /// If you don't specify layout, uses this as a default: %d [%t] %-5p %c [%x] &lt;%X{auth}&gt; - %m%n
        /// </summary>
        public static Configure Log4Net(this Configure config, AppenderSkeleton appender)
        {
            Log4Net();

            if (appender.Layout == null)
                appender.Layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
            if (appender.Threshold == null)
            {
                var cfg = ConfigurationManager.GetSection(typeof(Config.Logging).Name) as Config.Logging;
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
                else
                    appender.Threshold = Level.Warn;
            }

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
            props["configType"] = "EXTERNAL";
            Common.Logging.LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(props);
        }

        /// <summary>
        /// Configure NServiceBus to use Log4Net and specify your own configuration.
        /// </summary>
        public static void Log4Net(Action config)
        {
            Log4Net();

            config();
        }
    }
}
