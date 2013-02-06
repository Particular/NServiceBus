namespace NServiceBus.Logging.Loggers.NLogAdapter
{
    using System;
    using Internal;

    /// <summary>
    /// 
    /// </summary>
    public class NLogLoggerFactory : ILoggerFactory
    {
        private readonly Func<string, object> GetLoggerByStringDelegate;

        public NLogLoggerFactory()
        {
            var logManagerType = Type.GetType("NLog.LogManager, NLog");

            if (logManagerType == null)
                throw new InvalidOperationException("NLog could not be loaded. Make sure that the NLog assembly is located in the executable directory.");

            GetLoggerByStringDelegate = logManagerType.GetStaticFunctionDelegate<String, object>("GetLogger");
        }

        public ILog GetLogger(Type type)
        {
            return new NLogLogger(GetLoggerByStringDelegate(type.FullName));
        }

        public ILog GetLogger(string name)
        {
            var logger = GetLoggerByStringDelegate(name);
            return new NLogLogger(logger);
        }
    }
}