using System;
using System.Diagnostics;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace NServiceBus.Integration.Azure
{
    public sealed class AzureAppender : AppenderSkeleton
    {
        // new keys to integrate better with azure sdk 1.3 and up
        private const string ConnectionStringKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
        private const string LevelKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.Level";
        private const string LayoutKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.Layout";
        private const string ScheduledTransferPeriodKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.ScheduledTransferPeriod";
        private const string EventLogsKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.EventLogs";

        // ols keys to remain backward compatible
        private const string OldConnectionStringKey = "Diagnostics.ConnectionString";
        private const string OldLevelKey = "Diagnostics.Level";
        private const string OldLayoutKey = "Diagnostics.Layout";
        private const string OldScheduledTransferPeriodKey = "Diagnostics.ScheduledTransferPeriod";
        private const string OldEventLogsKey = "Diagnostics.EventLogs";


        public AzureAppender() : this(true)
        {
        }

        public AzureAppender(bool initializeDiagnostics)
        {
            this.InitializeDiagnostics = initializeDiagnostics;
            ScheduledTransferPeriod = GetScheduledTransferPeriod();
            Layout = new log4net.Layout.PatternLayout(GetLayout());
            Level = GetLevel();
        }

        public int ScheduledTransferPeriod { get; set; }

        public string Level { get; set; }

        public bool InitializeDiagnostics { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var logString = RenderLoggingEvent(loggingEvent);

            Trace.WriteLine(logString);
        }

        public override void ActivateOptions()
        {
            ConfigureThreshold();

            base.ActivateOptions();

            ConfigureAzureDiagnostics();
        }

        private void ConfigureThreshold()
        {
            if (!InitializeDiagnostics) return;

            var rootRepository = (Hierarchy)log4net.LogManager.GetRepository();
            Threshold = rootRepository.LevelMap[Level];
        }

        private void ConfigureAzureDiagnostics()
        {
            if (!RoleEnvironment.IsAvailable) return;

            Trace.Listeners.Add(new DiagnosticMonitorTraceListener());

            if (!InitializeDiagnostics) return;
            
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
            else
            {
                ConfigureDiagnostics(configuration);

                roleInstanceDiagnosticManager.SetCurrentConfiguration(configuration);
            }
            
        }

        private void ConfigureDiagnostics(DiagnosticMonitorConfiguration configuration)
        {
            //set threshold to verbose, what gets logged is controled by the log4net level
            configuration.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            ScheduleTransfer(configuration);

            ConfigureWindowsEventLogsToBeTransferred(configuration);
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

        private static string GetConnectionString()
        {
            try
            {
                try
                {
                    return RoleEnvironment.GetConfigurationSettingValue(ConnectionStringKey);
                }
                catch (Exception)
                {
                    return RoleEnvironment.GetConfigurationSettingValue(OldConnectionStringKey);
                }

            }
            catch (Exception)
            {
                return "UseDevelopmentStorage=true";
            }
        }


        private static string GetLevel()
        {
            try
            {
                try
                {
                    return RoleEnvironment.GetConfigurationSettingValue(LevelKey);
                }
                catch (Exception)
                {
                    return RoleEnvironment.GetConfigurationSettingValue(OldLevelKey);
                }
                
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
                try
                {
                    return RoleEnvironment.GetConfigurationSettingValue(LayoutKey);
                }
                catch (Exception)
                {
                    return RoleEnvironment.GetConfigurationSettingValue(OldLayoutKey);
                }
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
                try
                {
                    return int.Parse(RoleEnvironment.GetConfigurationSettingValue(ScheduledTransferPeriodKey));
                }
                catch (Exception)
                {
                    return int.Parse(RoleEnvironment.GetConfigurationSettingValue(OldScheduledTransferPeriodKey));
                }
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
                try
                {
                    return RoleEnvironment.GetConfigurationSettingValue(EventLogsKey);
                }
                catch (Exception)
                {
                    return RoleEnvironment.GetConfigurationSettingValue(OldEventLogsKey);
                }
            }
            catch (Exception)
            {
                return "Application!*;System!*";
            }
        }
    }
}

           