namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Diagnostics;
    using AcceptanceTesting;
    using Logging;

    public class ContextAppender : ILoggerFactory, ILog
    {
        public ContextAppender(ScenarioContext context, string endpointName)
        {
            this.context = context;
            this.endpointName = endpointName;
        }

        void Append(Exception exception)
        {
            lock (context)
            {
                context.Exceptions += exception + "/n/r";
            }
        }

        ScenarioContext context;
        readonly string endpointName;

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
        }

        public void Debug(string message, Exception exception)
        {
            Trace.WriteLine(string.Format("{0} {1}", message, exception));
            Append(exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format,args));
        }

        public void Info(string message)
        {
            Trace.WriteLine(message);
        }

        public void Info(string message, Exception exception)
        {
            Trace.WriteLine(string.Format("{0} {1}", message, exception));
            Append(exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }

        public void Warn(string message)
        {
            Trace.WriteLine(message);
        }

        public void Warn(string message, Exception exception)
        {
            Trace.WriteLine(string.Format("{0} {1}", message, exception));
            Append(exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }

        public void Error(string message)
        {
            Trace.WriteLine(message);

            context.RecordEndpointLog(endpointName,"error", message);
        }

        public void Error(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            context.RecordEndpointLog(endpointName, "error", fullMessage);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            context.RecordEndpointLog(endpointName, "error", fullMessage);
        }

        public void Fatal(string message)
        {
            Trace.WriteLine(message);
        }

        public void Fatal(string message, Exception exception)
        {
            Trace.WriteLine(string.Format("{0} {1}", message, exception));
            Append(exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }
    }
}