namespace NServiceBus.Logging
{
    using System;
    using System.Configuration;

    static class LogLevelReader
    {
        public static LogLevel GetDefaultLogLevel(LogLevel fallback = LogLevel.Info)
        {
            var logging = ConfigurationManager.GetSection(typeof(Config.Logging).Name) as Config.Logging;
            if (logging != null)
            {
                var threshold = logging.Threshold;
                LogLevel logLevel;
                if (!Enum.TryParse(threshold, true, out logLevel))
                {
                    var logLevels = string.Join(", ", Enum.GetNames(typeof(LogLevel)));
                    var message = string.Format("The value of '{0}' is invalid as a loglevel. Must be one of {1}.", threshold, logLevels);
                    throw new Exception(message);
                }
                return logLevel;
            }
            return fallback;
        }
    }
}