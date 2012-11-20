namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    using System;
    using Internal;

    /// <summary>
    /// 
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private static readonly Func<string, object> GetLoggerByStringDelegate;

        static LoggerFactory()
        {
            var logManagerType = Type.GetType("NLog.LogManager, NLog");

            if (logManagerType == null)
                throw new InvalidOperationException("Log4net could not be loaded. Make sure that the log4net assembly is located in the executable directory.");

            GetLoggerByStringDelegate = logManagerType.GetStaticFunctionDelegate<String, object>("GetLogger");
        }

        public ILog GetLogger(Type type)
        {
            return new Log(GetLoggerByStringDelegate(type.FullName));
        }

        public ILog GetLogger(string name)
        {
            object logger = GetLoggerByStringDelegate(name);
            return new Log(logger);
        }
    }
}