//TODO:Does not belong in NSB. breaks the idea of switchable logging libraries

//namespace NServiceBus.Integration.Azure
//{
//    using System;
//    using System.Diagnostics;
//    using Microsoft.WindowsAzure;
//    using Microsoft.WindowsAzure.Diagnostics;
//    using Microsoft.WindowsAzure.Diagnostics.Management;
//    using Microsoft.WindowsAzure.ServiceRuntime;
//    using log4net.Appender;
//    using log4net.Core;
//    using log4net.Repository.Hierarchy;

//    public sealed class AzureAppender : AppenderSkeleton
//    {
//        private const string ConnectionStringKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
//        private const string LevelKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.Level";
//        private const string LayoutKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.Layout";
//        private const string ScheduledTransferPeriodKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.ScheduledTransferPeriod";
//        private const string EventLogsKey = "Microsoft.WindowsAzure.Plugins.Diagnostics.EventLogs";

//        public AzureAppender() : this(true)
//        {
//        }

//        public AzureAppender(bool initializeDiagnostics)
//        {
//            this.InitializeDiagnostics = initializeDiagnostics;
//            ScheduledTransferPeriod = GetScheduledTransferPeriod();
//            Layout = new log4net.Layout.PatternLayout(GetLayout());
//            Level = GetLevel();
//        }

//        public int ScheduledTransferPeriod { get; set; }

//        public string Level { get; set; }

//        public bool InitializeDiagnostics { get; set; }

//        protected override void Append(LoggingEvent loggingEvent)
//        {
//            var logString = RenderLoggingEvent(loggingEvent);

//            if (loggingEvent.Level == log4net.Core.Level.Critical || loggingEvent.Level == log4net.Core.Level.Error || loggingEvent.Level == log4net.Core.Level.Emergency || loggingEvent.Level == log4net.Core.Level.Fatal || loggingEvent.Level == log4net.Core.Level.Severe)
//            {
//                Trace.TraceError(logString);
//            }
//            else if (loggingEvent.Level == log4net.Core.Level.Warn || loggingEvent.Level == log4net.Core.Level.Alert)
//            {
//                Trace.TraceWarning(logString);
//            }
//            else if (loggingEvent.Level == log4net.Core.Level.Info)
//            {
//                Trace.TraceInformation(logString);
//            }
//            else
//            {
//                Trace.WriteLine(logString);
//            }
//        }
        
//        public override void ActivateOptions()
//        {
//            ConfigureThreshold();

//            base.ActivateOptions();

//            ConfigureAzureDiagnostics();
//        }

//        private void ConfigureThreshold()
//        {
//            var rootRepository = (Hierarchy)log4net.LogManager.GetRepository();
//            Threshold = rootRepository.LevelMap[Level];
//        }

//        private void ConfigureAzureDiagnostics()
//        {
//            if (!RoleEnvironment.IsAvailable) return;

//            Trace.Listeners.Add(new DiagnosticMonitorTraceListener());

//            if (!InitializeDiagnostics) return;
            
//            var cloudStorageAccount = CloudStorageAccount.Parse(GetConnectionString());

//            var roleInstanceDiagnosticManager = cloudStorageAccount.CreateRoleInstanceDiagnosticManager(
//                                                    RoleEnvironment.DeploymentId,
//                                                    RoleEnvironment.CurrentRoleInstance.Role.Name,
//                                                    RoleEnvironment.CurrentRoleInstance.Id);

//            var configuration = roleInstanceDiagnosticManager.GetCurrentConfiguration();

//            if (configuration == null) // to remain backward compatible with sdk 1.2
//            {
//                configuration = DiagnosticMonitor.GetDefaultInitialConfiguration();
               
//                ConfigureDiagnostics(configuration);

//                DiagnosticMonitor.Start(cloudStorageAccount, configuration);
//            }
//        }

//        private void ConfigureDiagnostics(DiagnosticMonitorConfiguration configuration)
//        {
//            //set threshold to verbose, what gets logged is controled by the log4net level
//            configuration.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

//            ScheduleTransfer(configuration);

//            ConfigureWindowsEventLogsToBeTransferred(configuration);
//        }

//        private void ScheduleTransfer(DiagnosticMonitorConfiguration dmc)
//        {
//            var transferPeriod = TimeSpan.FromMinutes(ScheduledTransferPeriod);
//            dmc.Logs.ScheduledTransferPeriod = transferPeriod;
//            dmc.WindowsEventLog.ScheduledTransferPeriod = transferPeriod;
//        }

//        private static void ConfigureWindowsEventLogsToBeTransferred(DiagnosticMonitorConfiguration dmc)
//        {
//            var eventLogs = GetEventLogs().Split(';');
//            foreach (var log in eventLogs)
//            {
//                dmc.WindowsEventLog.DataSources.Add(log);
//            }
//        }

//        private static string GetConnectionString()
//        {
//            try
//            {
//                return RoleEnvironment.GetConfigurationSettingValue(ConnectionStringKey);
//            }
//            catch (Exception)
//            {
//                return "UseDevelopmentStorage=true";
//            }
//        }


//        private static string GetLevel()
//        {
//            try
//            {
//               return RoleEnvironment.GetConfigurationSettingValue(LevelKey);
//            }
//            catch (Exception)
//            {
//                return "Warn";
//            }
//        }

//        private static string GetLayout()
//        {
//            try
//            {
//               return RoleEnvironment.GetConfigurationSettingValue(LayoutKey);
//            }
//            catch (Exception)
//            {
//                return "%d [%t] %-5p %c [%x] <%X{auth}> - %m%n";
//            }
//        }

//        private static int GetScheduledTransferPeriod()
//        {
//            try
//            {
//              return int.Parse(RoleEnvironment.GetConfigurationSettingValue(ScheduledTransferPeriodKey));
//            }
//            catch (Exception)
//            {
//                return 10;
//            }
//        }

//        private static string GetEventLogs()
//        {
//            try
//            {
//                return RoleEnvironment.GetConfigurationSettingValue(EventLogsKey);
//            }
//            catch (Exception)
//            {
//                return "Application!*;System!*";
//            }
//        }

//    }
//}

           