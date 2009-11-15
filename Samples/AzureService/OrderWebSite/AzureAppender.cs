using log4net.Appender;
using log4net.Core;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace OrderWebSite
{
    public class AzureAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            RoleManager.WriteToLog(GetLogName(loggingEvent), loggingEvent.RenderedMessage);
        }

        private static string GetLogName(LoggingEvent loggingEvent)
        {
            if (loggingEvent.Level == Level.Critical)
                return EventLogNames.Critical;

            if (loggingEvent.Level == Level.Error)
                return EventLogNames.Error;

            if (loggingEvent.Level == Level.Warn)
                return EventLogNames.Warning;

            if (loggingEvent.Level == Level.Info)
                return EventLogNames.Information;

            if (loggingEvent.Level == Level.Verbose)
                return EventLogNames.Verbose;

            return EventLogNames.Error;
        }

        private static class EventLogNames
        {
            public const string Critical = "Critical";
            public const string Error = "Error";
            public const string Warning = "Warning";
            public const string Information = "Information";
            public const string Verbose = "Verbose";
        }
    }
}