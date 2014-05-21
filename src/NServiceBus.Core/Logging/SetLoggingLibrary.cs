// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Configuration;
    using Logging;

    /// <summary>
    /// Class containing extension method to allow users to use Log4Net for logging
    /// </summary>
    public static class SetLoggingLibrary
    {

        [ObsoleteEx(RemoveInVersion = "6.0")]
        public static Configure Log4Net(this Configure config)
        {
            throw new Exception("TODO");
        }

        [ObsoleteEx(RemoveInVersion = "6.0")]
        public static Configure Log4Net<TAppender>(this Configure config, Action<TAppender> initializeAppender) where TAppender : new()
        {
            throw new Exception("TODO");
        }

        [ObsoleteEx(RemoveInVersion = "6.0")]
        public static Configure Log4Net(this Configure config, object appenderSkeleton)
        {
            throw new Exception("TODO");
        }


        [ObsoleteEx(RemoveInVersion = "6.0")]
        public static void Log4Net()
        {
            throw new Exception("TODO");
        }

        [ObsoleteEx(RemoveInVersion = "6.0")]
        public static void Log4Net(Action config)
        {
            throw new Exception("TODO");
        }

        [ObsoleteEx(RemoveInVersion = "6.0")]
        public static Configure NLog(this Configure config, params object[] targetsForNServiceBusToLogTo)
        {
            throw new Exception("TODO");
        }

        [ObsoleteEx(RemoveInVersion = "6.0")]
        public static void NLog()
        {
            throw new Exception("TODO");
        }

        [ObsoleteEx(RemoveInVersion = "6.0")]
        public static void Custom(ILoggerFactory loggerFactory)
        {
            LogManager.LoggerFactory = loggerFactory;
        }

        public static void ConfigureDefaults(LogLevel level=LogLevel.Info, string loggingDirectory = null)
        {
            LogLevel levelFromConfig;
            if (TryGetThresholdFromConfig(out levelFromConfig))
            {
                level = levelFromConfig;
            }

            LogManager.LoggerFactory = new DefaultLoggerFactory(level, loggingDirectory);
        }

        static bool TryGetThresholdFromConfig(out LogLevel logLevel)
        {
            var logging = ConfigurationManager.GetSection(typeof(Config.Logging).Name) as Config.Logging;
            if (logging != null)
            {
                var threshold = logging.Threshold;
                if (!Enum.TryParse(threshold, true, out logLevel))
                {
                    var logLevels = string.Join(", ",Enum.GetNames(typeof(LogLevel)));
                    var message = string.Format("The value of '{0}' is invalid as a loglevel. Must be one of {1}.", threshold, logLevels);
                    throw new Exception(message);
                }
                return true;
            }
            logLevel = LogLevel.Debug;
            return false;
        }
        //TODO: perhaps add method for null logging

    }
}