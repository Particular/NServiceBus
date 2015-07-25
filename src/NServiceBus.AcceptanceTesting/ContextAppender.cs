namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Diagnostics;
    using Logging;

    class ContextAppender : ILoggerFactory, ILog
    {
        void Append(Exception exception)
        {
            lock (context)
            {
                context.Exceptions += exception + "/n/r";
            }
        }

        public static void SetContext(ScenarioContext newContext)
        {
            context = newContext;
        }

        static ScenarioContext context;

        public ILog GetLogger(Type type)
        {
            return this;
        }

        public ILog GetLogger(string name)
        {
            return this;
        }

        public bool IsDebugEnabled { get{return true;}}
        public bool IsInfoEnabled { get { return true; } }
        public bool IsWarnEnabled { get { return true; } }
        public bool IsErrorEnabled { get { return true; } }
        public bool IsFatalEnabled { get { return true; } }

        public void Debug(string message)
        {
            Trace.WriteLine(message);
            context.RecordEndpointLog("warn", message);
        }

        public void Debug(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            context.RecordEndpointLog("warn", fullMessage);
        }

        public void DebugFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format,args);
            Trace.WriteLine(fullMessage);
            context.RecordEndpointLog("warn", fullMessage);
        }

        public void Info(string message)
        {
            Trace.WriteLine(message);
            context.RecordEndpointLog("warn", message);
        }

        public void Info(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            context.RecordEndpointLog("warn", fullMessage);
        }

        public void InfoFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            context.RecordEndpointLog("warn", fullMessage);
        }

        public void Warn(string message)
        {
            Trace.WriteLine(message);
            context.RecordEndpointLog("warn", message);
        }

        public void Warn(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            context.RecordEndpointLog("warn", fullMessage);
        }

        public void WarnFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            context.RecordEndpointLog("warn", fullMessage);
        }

        public void Error(string message)
        {
            Trace.WriteLine(message);

            context.RecordEndpointLog("error", message);
        }

        public void Error(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            context.RecordEndpointLog("error", fullMessage);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            context.RecordEndpointLog("error", fullMessage);
        }

        public void Fatal(string message)
        {
            Trace.WriteLine(message);

            context.RecordEndpointLog("error", message);
        }

        public void Fatal(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            context.RecordEndpointLog("fatal", fullMessage);
        }

        public void FatalFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            context.RecordEndpointLog("fatal", fullMessage);
        }
    }
}