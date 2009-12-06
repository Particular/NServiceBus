using System;
using log4net.Appender;
using log4net.Core;
using Microsoft.WindowsAzure.Diagnostics;

namespace NServiceBus.Integration.Azure
{
    public class AzureAppender : AppenderSkeleton
    {
        public AzureAppender()
        {
            ScheduledTransferPeriod = TimeSpan.FromMinutes(1);
            Layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");

            ConnectionStringKey = "DiagnosticsStorage.ConnectionString";
        }

        public TimeSpan ScheduledTransferPeriod { get; set; }
        public string ConnectionStringKey { get; set; }

        public override void ActivateOptions()
        {
            base.ActivateOptions();
            ConfigureAzureDiagnostics();
        }


        protected override void Append(LoggingEvent loggingEvent)
        {

            var logString = RenderLoggingEvent(loggingEvent);

            System.Diagnostics.Trace.WriteLine(logString);
        }

        private void ConfigureAzureDiagnostics()
        {
            var dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();

            dmc.Logs.ScheduledTransferPeriod = ScheduledTransferPeriod;

            //set threshold to verbose, what gets logged is controled by the log4net threshold anyway
            dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            DiagnosticMonitor.Start(ConnectionStringKey, dmc);
        }

    }
}