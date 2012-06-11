using System;
using NServiceBus.Logging.Internal;

namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    /// <summary>
    /// 
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private static readonly Func<Type, object> GetLoggerByTypeDelegate;
        private static readonly Func<String, object> GetLoggerByStringDelegate;

        static LoggerFactory()
        {
            var logManagerType = Type.GetType("log4net.LogManager, log4net");

            if (logManagerType == null)
                throw new InvalidOperationException("Log4net could not be loaded. Make sure that the log4net assembly is located in the executable directory.");

            GetLoggerByTypeDelegate = logManagerType.GetStaticFunctionDelegate<Type, object>("GetLogger");
            GetLoggerByStringDelegate = logManagerType.GetStaticFunctionDelegate<String, object>("GetLogger");
        }

        public ILog GetLogger(Type type)
        {
            return new Log(GetLoggerByTypeDelegate(type));
        }

        public ILog GetLogger(string name)
        {
            return new Log(GetLoggerByStringDelegate(name));
        }
    }
}