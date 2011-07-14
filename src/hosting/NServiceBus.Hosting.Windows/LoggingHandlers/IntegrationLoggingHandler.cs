using log4net.Appender;
using log4net.Core;

namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    /// <summary>
    /// Handles logging configuration for the integration profile.
    /// </summary>
    public class IntegrationLoggingHandler : IConfigureLoggingForProfile<Integration>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            SetLoggingLibrary.Log4Net<ColoredConsoleAppender>(null,
                a =>
                {
                    LiteLoggingHandler.PrepareColors(a);
                    a.Threshold = Level.Info;
                }
            );
        }
    }
}