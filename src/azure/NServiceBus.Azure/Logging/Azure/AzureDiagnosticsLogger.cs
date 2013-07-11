using System;
using System.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics;
using NServiceBus.Logging;

namespace NServiceBus.Integration.Azure
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureDiagnosticsLogger : ILog
    {
        /// <summary>
        /// 
        /// </summary>
        public bool IsDebugEnabled
        {
            get { return true; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsInfoEnabled
        {
            get { return true; }
        }

        public bool IsWarnEnabled
        {
            get { return true; }
        }

        public bool IsErrorEnabled
        {
            get { return true; }
        }

        public bool IsFatalEnabled
        {
            get { return true; }
        }

        public LogLevel Level { get; set; }

        public void Debug(string message)
        {
            Info(message);
        }

        public void Debug(string message, Exception exception)
        {
            Info(message, exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            InfoFormat(format, args);
        }

        public void Info(string message)
        {
            if (TracingInfo)
            {
                Trace.TraceInformation(message);
            }
        }

        public void Info(string message, Exception exception)
        {
            if (TracingInfo)
            {
                Trace.TraceInformation(message);
                Trace.TraceInformation(exception.ToString());
            }
        }

        public void InfoFormat(string format, params object[] args)
        {
            if (TracingInfo)
            {
                Trace.TraceInformation(format, args);
            }
        }

        public void Warn(string message)
        {
            if (TracingWarnings)
            {
                Trace.TraceWarning(message);
            }
        }

        public void Warn(string message, Exception exception)
        {
            if (TracingWarnings)
            {
                Trace.TraceWarning(message);
                Trace.TraceWarning(exception.ToString());
            }
        }

        public void WarnFormat(string format, params object[] args)
        {
            if (TracingWarnings)
            {
                Trace.TraceWarning(format, args);
            }
        }

        public void Error(string message)
        {
            if (TracingErrors)
            {
                Trace.TraceError(message);
            }
        }

        public void Error(string message, Exception exception)
        {
            if (TracingErrors)
            {
                Trace.TraceError(message);
                Trace.TraceError(exception.ToString());
            }
        }

        public void ErrorFormat(string format, params object[] args)
        {
            if (TracingErrors)
            {
                Trace.TraceError(format, args);
            }
        }

        public void Fatal(string message)
        {
            if (TracingErrors)
            {
                Trace.TraceError(message);
            }
        }

        public void Fatal(string message, Exception exception)
        {
            if (TracingErrors)
            {
                Trace.TraceError(message);
                Trace.TraceError(exception.ToString());
            }
        }

        public void FatalFormat(string format, params object[] args)
        {
            if (TracingErrors)
            {
                Trace.TraceError(format, args);
            }
        }

        private bool TracingErrors
        {
            get { return Level == LogLevel.Critical || Level == LogLevel.Error ||Level == LogLevel.Warning || Level == LogLevel.Undefined || Level == LogLevel.Verbose ||Level == LogLevel.Information; }
        }

        private bool TracingWarnings
        {
            get { return Level == LogLevel.Warning || Level == LogLevel.Undefined || Level == LogLevel.Verbose || Level == LogLevel.Information; }
        }

        private bool TracingInfo
        {
            get { return Level == LogLevel.Undefined || Level == LogLevel.Verbose || Level == LogLevel.Information; }
        }
    }
}