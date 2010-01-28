using log4net.Appender;
using log4net.Core;

namespace NServiceBus.Host.Internal.Logging
{
    /// <summary>
    /// Handles logging configuration for the lite profile.
    /// </summary>
    public class LiteLoggingHandler : IConfigureLoggingForProfile<Lite>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            NServiceBus.SetLoggingLibrary.Log4Net<ConsoleAppender>(null, ca => ca.Threshold = Level.Debug);
        }
    }
}
