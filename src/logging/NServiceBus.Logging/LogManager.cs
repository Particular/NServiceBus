using System;
using NServiceBus.Logging.Loggers;

namespace NServiceBus.Logging
{
    /// <summary>
    /// 
    /// </summary>
    public class LogManager
    {
        private static ILoggerFactory _loggerFactory = new NullLoggerFactory();

        /// <summary>
        /// 
        /// </summary>
        public static ILoggerFactory LoggerFactory
        {
            get { return _loggerFactory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _loggerFactory = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ILog GetLogger(Type type)
        {
            return LoggerFactory.GetLogger(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ILog GetLogger(string name)
        {
            return LoggerFactory.GetLogger(name);
        }
    }
}