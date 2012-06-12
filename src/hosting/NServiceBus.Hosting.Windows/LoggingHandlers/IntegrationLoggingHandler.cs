
using System.Configuration;

namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    /// <summary>
    /// Handles logging configuration for the integration profile.
    /// </summary>
    public class IntegrationLoggingHandler : IConfigureLoggingForProfile<Integration>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            if (SetLoggingLibrary.Log4NetExists)
                SetLoggingLibrary.Log4Net(null, Logging.Loggers.Log4NetAdapter.AppenderFactory.CreateColoredConsoleAppender("Info"));
            else if (SetLoggingLibrary.NLogExists)
                SetLoggingLibrary.NLog(null, Logging.Loggers.NLogAdapter.TargetFactory.CreateColoredConsoleTarget());
            else
                Internal.ConfigureInternalLog4Net.Integration();
            //                throw new ConfigurationErrorsException("No logging framework found. NServiceBus supports log4net and NLog. You need to put any of these in the same directory as the host.");
        }
    }
}