namespace NServiceBus.Logging.Loggers.Log4NetAdapter
{
    using System;
    using Internal;

    /// <summary>
    /// 
    /// </summary>
    public class Log4NetLoggerFactory : ILoggerFactory
    {
        private readonly Func<Type, object> GetLoggerByTypeDelegate;
        private readonly Func<String, object> GetLoggerByStringDelegate;

        public Log4NetLoggerFactory()
        {
            var logManagerType = Type.GetType("log4net.LogManager, log4net");

            if (logManagerType == null)
                throw new InvalidOperationException("Log4net could not be loaded. Make sure that the log4net assembly is located in the executable directory.");

            GetLoggerByTypeDelegate = logManagerType.GetStaticFunctionDelegate<Type, object>("GetLogger");
            GetLoggerByStringDelegate = logManagerType.GetStaticFunctionDelegate<String, object>("GetLogger");
        }

        public ILog GetLogger(Type type)
        {
            return new Log4NetLogger(GetLoggerByTypeDelegate(type));
        }

        public ILog GetLogger(string name)
        {
            return new Log4NetLogger(GetLoggerByStringDelegate(name));
        }
    }
}