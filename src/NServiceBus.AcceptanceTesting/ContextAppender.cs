namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Diagnostics;
    using Logging;

    /// <summary>
    /// This class is written under the assumption that acceptance tests are executed sequentially.
    /// </summary>
    class ContextAppender : ILoggerFactory, ILog
    {
        void Append(Exception exception)
        {
            lock (Context)
            {
                Context.Exceptions += exception + "/n/r";
            }
        }

        /// <summary>
        /// Because ILoggerFactory interface methods are only used in a static context. This is the only way to set the currently executing context.
        /// </summary>
        /// <param name="newContext">The new context to be set</param>
        public static void SetContext(ScenarioContext newContext)
        {
            lock (context)
            {
                context = newContext;
            }
        }

        static ScenarioContext context;

        static ScenarioContext Context
        {
            get
            {
                if (context == null)
                {
                    throw new InvalidOperationException("You have to set a context first by calling SetContext(ScenarioContext newContext).");
                }
                return context;
            }
        }

        public ILog GetLogger(Type type)
        {
            return this;
        }

        public ILog GetLogger(string name)
        {
            return this;
        }

        public bool IsDebugEnabled { get { return true; } }
        public bool IsInfoEnabled { get { return true; } }
        public bool IsWarnEnabled { get { return true; } }
        public bool IsErrorEnabled { get { return true; } }
        public bool IsFatalEnabled { get { return true; } }

        public void Debug(string message)
        {
            Trace.WriteLine(message);
            Context.RecordEndpointLog("warn", message);
        }

        public void Debug(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            Context.RecordEndpointLog("warn", fullMessage);
        }

        public void DebugFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            Context.RecordEndpointLog("warn", fullMessage);
        }

        public void Info(string message)
        {
            Trace.WriteLine(message);
            Context.RecordEndpointLog("warn", message);
        }

        public void Info(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            Context.RecordEndpointLog("warn", fullMessage);
        }

        public void InfoFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            Context.RecordEndpointLog("warn", fullMessage);
        }

        public void Warn(string message)
        {
            Trace.WriteLine(message);
            Context.RecordEndpointLog("warn", message);
        }

        public void Warn(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            Context.RecordEndpointLog("warn", fullMessage);
        }

        public void WarnFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            Context.RecordEndpointLog("warn", fullMessage);
        }

        public void Error(string message)
        {
            Trace.WriteLine(message);

            Context.RecordEndpointLog("error", message);
        }

        public void Error(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            Context.RecordEndpointLog("error", fullMessage);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            Context.RecordEndpointLog("error", fullMessage);
        }

        public void Fatal(string message)
        {
            Trace.WriteLine(message);

            Context.RecordEndpointLog("error", message);
        }

        public void Fatal(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            Append(exception);
            Context.RecordEndpointLog("fatal", fullMessage);
        }

        public void FatalFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            Context.RecordEndpointLog("fatal", fullMessage);
        }
    }
}