using System;
using NServiceBus.Logging;

namespace NServiceBus
{
    public static class ConfigureLogging
    {
        /// <summary>
        /// Use Log4Net for logging
        /// </summary>
        public static Configure Log4Net(this Configure config)
        {
            SetLoggingLibrary.Log4Net();
            return config;
        }
        
        public static Configure NLog(this Configure config)
        {
            SetLoggingLibrary.NLog();
            return config;
        }

        public static Configure Custom(this Configure config, ILoggerFactory loggerFactory)
        {
            SetLoggingLibrary.Custom(loggerFactory);
            return config;
        }
    }
}