namespace NServiceBus.Host.Internal.Logging
{
    /// <summary>
    /// Handles logging configuration for the production profile
    /// </summary>
    public class ProductionLoggingHandler : IConfigureLoggingForProfile<Production>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            var layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
            var level = log4net.Core.Level.Warn;

            var appender = new log4net.Appender.RollingFileAppender
            {
                Layout = layout,
                Threshold = level, 
                CountDirection = 1,
                DatePattern = "yyyy-MM-dd",
                RollingStyle = log4net.Appender.RollingFileAppender.RollingMode.Composite,
                MaxFileSize = 1024*1024,
                MaxSizeRollBackups = 10,
                LockingModel = new log4net.Appender.FileAppender.MinimalLock(),
                StaticLogFileName = true,
                File = "logfile",
                AppendToFile = true
            };
            appender.ActivateOptions();

            log4net.Config.BasicConfigurator.Configure(appender);
        }
    }
}
