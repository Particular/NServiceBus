namespace NServiceBus.NLog
{
    using System;
    using Logging;

    class Logger : ILog
    {
        global::NLog.Logger logger;

        public Logger(global::NLog.Logger logger)
        {
            this.logger = logger;
        }

        public void Debug(string message)
        {
            logger.Debug(message);
        }

        public void Debug(string message, Exception exception)
        {
            logger.DebugException(message, exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            logger.Debug(format, args);
        }

        public void Info(string message)
        {
            logger.Info(message);
        }

        public void Info(string message, Exception exception)
        {
            logger.InfoException(message, exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            logger.Info(format, args);
        }

        public void Warn(string message)
        {
            logger.Warn(message);
        }

        public void Warn(string message, Exception exception)
        {
            logger.WarnException(message, exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            logger.Warn(format, args);
        }

        public void Error(string message)
        {
            logger.Error(message);
        }

        public void Error(string message, Exception exception)
        {
            logger.ErrorException(message, exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            logger.Error(format, args);
        }

        public void Fatal(string message)
        {
            logger.Fatal(message);
        }

        public void Fatal(string message, Exception exception)
        {
            logger.FatalException(message, exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            logger.Fatal(format, args);
        }

        public bool IsDebugEnabled
        {
            get { return logger.IsDebugEnabled; }
        }

        public bool IsInfoEnabled
        {
            get { return logger.IsInfoEnabled; }
        }

        public bool IsWarnEnabled
        {
            get { return logger.IsWarnEnabled; }
        }

        public bool IsErrorEnabled
        {
            get { return logger.IsErrorEnabled; }
        }

        public bool IsFatalEnabled
        {
            get { return logger.IsFatalEnabled; }
        }
    }
}