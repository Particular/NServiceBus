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
        static object contextLocker = new object();

        /// <summary>
        /// Because ILoggerFactory interface methods are only used in a static context. This is the only way to set the currently executing context.
        /// </summary>
        /// <param name="newContext">The new context to be set</param>
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

        public bool IsDebugEnabled { get { return true; } }
        public bool IsInfoEnabled { get { return true; } }
        public bool IsWarnEnabled { get { return true; } }
        public bool IsErrorEnabled { get { return true; } }
        public bool IsFatalEnabled { get { return true; } }


        static void RecordLog(string message, string level)
        {
            lock (contextLocker)
            {
                if (context != null)
                {
                    context.RecordEndpointLog(level, message);
                }
            }
        }

        static void AppendException(Exception exception)
        {
            lock (contextLocker)
            {
                if (context != null)
                {
                    context.Exceptions += exception + Environment.NewLine;
                }
            }
        }

        public void Debug(string message)
        {
            Trace.WriteLine(message);
            RecordLog(message, "debug");
        }

        public void Debug(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            AppendException(exception);
            RecordLog(fullMessage, "debug");
        }

        public void DebugFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            RecordLog(fullMessage, "debug");
        }

        public void Info(string message)
        {
            Trace.WriteLine(message);
            RecordLog(message, "info");
        }


        public void Info(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            AppendException(exception);
            RecordLog(fullMessage, "info");
        }

        public void InfoFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            RecordLog(fullMessage, "info");
        }

        public void Warn(string message)
        {
            Trace.WriteLine(message);
            RecordLog(message, "warn");
        }

        public void Warn(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            AppendException(exception);
            RecordLog(fullMessage, "warn");
        }

        public void WarnFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            RecordLog(fullMessage, "warn");
        }

        public void Error(string message)
        {
            Trace.WriteLine(message);

            RecordLog(message, "error");
        }

        public void Error(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            AppendException(exception);
            RecordLog(fullMessage, "error");
        }

        public void ErrorFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            RecordLog(fullMessage, "error");
        }

        public void Fatal(string message)
        {
            Trace.WriteLine(message);

            RecordLog(message, "fatal");
        }

        public void Fatal(string message, Exception exception)
        {
            var fullMessage = string.Format("{0} {1}", message, exception);
            Trace.WriteLine(fullMessage);
            AppendException(exception);
            RecordLog(fullMessage, "fatal");
        }

        public void FatalFormat(string format, params object[] args)
        {
            var fullMessage = string.Format(format, args);
            Trace.WriteLine(fullMessage);
            RecordLog(fullMessage, "fatal");
        }
    }
}