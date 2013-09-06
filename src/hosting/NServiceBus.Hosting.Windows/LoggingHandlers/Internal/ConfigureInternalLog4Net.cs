namespace NServiceBus.Hosting.Windows.LoggingHandlers.Internal
{
    using System.Reflection;
    using log4net.Appender;
    using log4net.Core;
    using Logging;

    internal class ConfigureInternalLog4Net
    {
        public static void Lite()
        {
            ConfigureColoredConsoleAppender(Level.Info);

            LogManager.LoggerFactory = new InternalLog4NetLoggerFactory();
        }

        public static void Integration()
        {
            ConfigureColoredConsoleAppender(Level.Info);

            LogManager.LoggerFactory = new InternalLog4NetLoggerFactory();
        }

        public static void Production(bool logToConsole)
        {
            ConfigureFileAppender();

            if (logToConsole)
                ConfigureColoredConsoleAppender(Level.Info);

            LogManager.LoggerFactory = new InternalLog4NetLoggerFactory();
        }

        private static void ConfigureFileAppender()
        {
            var appender = new RollingFileAppender
                        {
                            CountDirection = 1,
                            DatePattern = "yyyy-MM-dd",
                            RollingStyle = RollingFileAppender.RollingMode.Composite,
                            MaxFileSize = 1024*1024,
                            MaxSizeRollBackups = 10,
                            LockingModel = new FileAppender.MinimalLock(),
                            StaticLogFileName = true,
                            File = "logfile",
                            AppendToFile = true
                        };

            ConfigureAppender(appender);
        }

        private static void ConfigureColoredConsoleAppender(Level threshold)
        {
            var appender = new ColoredConsoleAppender()
            {
                Threshold = threshold
            };

            appender.AddMapping(
                new ColoredConsoleAppender.LevelColors
                {
                    Level = Level.Debug,
                    ForeColor = ColoredConsoleAppender.Colors.White
                });
            appender.AddMapping(
                new ColoredConsoleAppender.LevelColors
                {
                    Level = Level.Info,
                    ForeColor = ColoredConsoleAppender.Colors.Green
                });
            appender.AddMapping(
                new ColoredConsoleAppender.LevelColors
                {
                    Level = Level.Warn,
                    ForeColor = ColoredConsoleAppender.Colors.Yellow | ColoredConsoleAppender.Colors.HighIntensity
                });
            appender.AddMapping(
                new ColoredConsoleAppender.LevelColors
                {
                    Level = Level.Error,
                    ForeColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity
                });

            ConfigureAppender(appender);
        }
        
        private static void ConfigureAppender(AppenderSkeleton appender)
        {
            if (appender.Layout == null)
                appender.Layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");

            var cfg = Configure.GetConfigSection<Config.Logging>();
            if (cfg != null)
            {
                foreach (var f in typeof(Level).GetFields(BindingFlags.Public | BindingFlags.Static))
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
        }
    }
}