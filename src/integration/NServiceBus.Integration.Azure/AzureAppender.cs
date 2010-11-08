using System;
using System.Diagnostics;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace NServiceBus.Integration.Azure
{
    public sealed class AzureAppender : AppenderSkeleton
    {
        private const string ConnectionStringKey = "Diagnostics.ConnectionString";
        private const string LevelKey = "Diagnostics.Level";
        private const string LayoutKey = "Diagnostics.Layout";
        private const string ScheduledTransferPeriodKey = "Diagnostics.ScheduledTransferPeriod";
        private const string EventLogsKey = "Diagnostics.EventLogs";

        public AzureAppender()
        {
            ScheduledTransferPeriod = GetScheduledTransferPeriod();
            Layout = new log4net.Layout.PatternLayout(GetLayout());
            Level = GetLevel();
        }

        public int ScheduledTransferPeriod { get; set; }

        public string Level { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var logString = RenderLoggingEvent(loggingEvent);

            System.Diagnostics.Trace.WriteLine(logString);
        }

        public override void ActivateOptions()
        {
            ConfigureThreshold();

            base.ActivateOptions();

            ConfigureAzureDiagnostics();
        }

        private void ConfigureThreshold()
        {
            var rootRepository = (Hierarchy)log4net.LogManager.GetRepository();
            Threshold = rootRepository.LevelMap[Level];
        }

        private void ConfigureAzureDiagnostics()
        {
            var traceListener = new DiagnosticMonitorTraceListener();
            Trace.Listeners.Add(traceListener);

            var dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();

            //set threshold to verbose, what gets logged is controled by the log4net level
            dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            ScheduleTransfer(dmc);

            ConfigureWindowsEventLogsToBeTransferred(dmc);

            DiagnosticMonitor.Start(ConnectionStringKey, dmc);
        }

        private void ScheduleTransfer(DiagnosticMonitorConfiguration dmc)
        {
            var transferPeriod = TimeSpan.FromMinutes(ScheduledTransferPeriod);
            dmc.Logs.ScheduledTransferPeriod = transferPeriod;
            dmc.WindowsEventLog.ScheduledTransferPeriod = transferPeriod;
        }

        private static void ConfigureWindowsEventLogsToBeTransferred(DiagnosticMonitorConfiguration dmc)
        {
            var eventLogs = GetEventLogs().Split(';');
            foreach (var log in eventLogs)
            {
                dmc.WindowsEventLog.DataSources.Add(log);
            }
        }

        private static string GetLevel()
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(LevelKey);
            }
            catch (Exception)
            {
                return "Error";
            }
        }

        private static string GetLayout()
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(LayoutKey);
            }
            catch (Exception)
            {
                return "%d [%t] %-5p %c [%x] <%X{auth}> - %m%n";
            }
        }

        private static int GetScheduledTransferPeriod()
        {
            try
            {
                return int.Parse(RoleEnvironment.GetConfigurationSettingValue(ScheduledTransferPeriodKey));
            }
            catch (Exception)
            {
                return 5;
            }
        }

        private static string GetEventLogs()
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(EventLogsKey);
            }
            catch (Exception)
            {
                return "Application!*;System!*";
            }
        }
    }
}

           