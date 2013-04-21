using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Logging;

namespace NServiceBus.Integration.Azure
{
    using System.Security;
    using Microsoft.WindowsAzure.Storage;
    using CloudStorageAccount = Microsoft.WindowsAzure.CloudStorageAccount;

    /// <summary>
    /// 
    /// </summary>
    public class AzureDiagnosticsLoggerFactory : ILoggerFactory
    {
        private const string ConnectionStringKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
        private const string LevelKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.Level";
        private const string ScheduledTransferPeriodKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.ScheduledTransferPeriod";
        private const string EventLogsKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.EventLogs";

        public int ScheduledTransferPeriod { get; set; }

        public string Level { get; set; }

        public bool InitializeDiagnostics { get; set; }

        public bool Enable { get; set; }

        public ILog GetLogger(Type type)
        {
            var logger = new AzureDiagnosticsLogger();
            logger.Level = GetLevel();
            return logger;
        }

        public ILog GetLogger(string name)
        {
            var logger = new AzureDiagnosticsLogger();
            logger.Level = GetLevel();
            return logger;
        }

        public void ConfigureAzureDiagnostics()
        {
            if (Enable)
            {
                var exists = Trace.Listeners.Cast<TraceListener>().Count(tracelistener => tracelistener.GetType().IsAssignableFrom(typeof(DiagnosticMonitorTraceListener))) > 0;
                if (!exists)
                {
                    try
                    {
                        var listener = new DiagnosticMonitorTraceListener();
                        Trace.Listeners.Add(listener);
                    }
                    catch (SecurityException)
                    {
                        return;
                    }
                    
                }
            }
            else
            {
                var exists = Trace.Listeners.Cast<TraceListener>().Count(tracelistener => tracelistener.GetType().IsAssignableFrom(typeof(ConsoleTraceListener))) > 0;
                if (!exists) Trace.Listeners.Add(new ConsoleTraceListener());
            }

            if (!RoleEnvironment.IsAvailable || !InitializeDiagnostics) return;

            var cloudStorageAccount = CloudStorageAccount.Parse(GetConnectionString());

            var roleInstanceDiagnosticManager = cloudStorageAccount.CreateRoleInstanceDiagnosticManager(
                RoleEnvironment.DeploymentId,
                RoleEnvironment.CurrentRoleInstance.Role.Name,
                RoleEnvironment.CurrentRoleInstance.Id);

            var configuration = roleInstanceDiagnosticManager.GetCurrentConfiguration();

            if (configuration == null) // to remain backward compatible with sdk 1.2
            {
                configuration = DiagnosticMonitor.GetDefaultInitialConfiguration();

                ConfigureDiagnostics(configuration);

                DiagnosticMonitor.Start(cloudStorageAccount, configuration);
            }
           
        }

        private void ConfigureDiagnostics(DiagnosticMonitorConfiguration configuration)
        {
            configuration.Logs.ScheduledTransferLogLevelFilter = GetLevel();

            ScheduleTransfer(configuration);

            ConfigureWindowsEventLogsToBeTransferred(configuration);
        }

        private void ScheduleTransfer(DiagnosticMonitorConfiguration dmc)
        {
            ScheduledTransferPeriod = GetScheduledTransferPeriod();
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

        private static string GetConnectionString()
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(ConnectionStringKey);
            }
            catch (Exception)
            {
                return "UseDevelopmentStorage=true";
            }
        }

        private static LogLevel GetLevel()
        {
            try
            {
                return (LogLevel)Enum.Parse(typeof(LogLevel), RoleEnvironment.GetConfigurationSettingValue(LevelKey));
            }
            catch (Exception)
            {
                return LogLevel.Warning;
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
                return 10;
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