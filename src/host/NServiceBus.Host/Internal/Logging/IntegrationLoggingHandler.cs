namespace NServiceBus.Host.Internal.Logging
{
    /// <summary>
    /// Handles logging configuration for the integration profile.
    /// </summary>
    public class IntegrationLoggingHandler : IConfigureLoggingForProfile<Integration>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            var layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
            var level = log4net.Core.Level.Info;

            var appender = new log4net.Appender.ConsoleAppender
            {
                Layout = layout,
                Threshold = level
            };
            log4net.Config.BasicConfigurator.Configure(appender);
        }
    }
}
