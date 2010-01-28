using log4net.Appender;
using log4net.Core;

namespace NServiceBus.Host.Internal.Logging
{
    /// <summary>
    /// Handles logging configuration for the production profile
    /// </summary>
    public class ProductionLoggingHandler : IConfigureLoggingForProfile<Production>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            NServiceBus.SetLoggingLibrary.Log4Net<RollingFileAppender>(null,
                a =>
                    {
                        a.CountDirection = 1;
                        a.DatePattern = "yyyy-MM-dd";
                        a.RollingStyle = RollingFileAppender.RollingMode.Composite;
                        a.MaxFileSize = 1024*1024;
                        a.MaxSizeRollBackups = 10;
                        a.LockingModel = new FileAppender.MinimalLock();
                        a.StaticLogFileName = true;
                        a.File = "logfile";
                        a.AppendToFile = true;
                    });

        }
    }
}
