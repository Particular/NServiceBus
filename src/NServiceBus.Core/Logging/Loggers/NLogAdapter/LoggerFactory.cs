namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    using System;
    using Internal;

    /// <summary>
    /// 
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private readonly Func<string, object> GetLoggerByStringDelegate;

        public LoggerFactory()
        {
            var logManagerType = Type.GetType("NLog.LogManager, NLog");

            if (logManagerType == null)
                throw new InvalidOperationException("NLog could not be loaded. Make sure that the NLog assembly is located in the executable directory.");

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