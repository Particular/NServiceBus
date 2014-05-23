namespace NServiceBus.Logging
{
    using System;
    using Loggers;

    public class LogManager
    {
        static ILoggerFactory loggerFactory = new NullLoggerFactory();

        public static bool IsConfigured { get { return loggerFactory.GetType() != typeof(NullLoggerFactory); } }

        public static ILoggerFactory LoggerFactory
        {
            get { return loggerFactory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                loggerFactory = value;
            }
        }

        public static ILog GetLogger(Type type)
        {
            return loggerFactory.GetLogger(type);
        }

        public static ILog GetLogger(string name)
        {
            return loggerFactory.GetLogger(name);
        }
    }
}