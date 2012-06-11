using System.Configuration;

namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    /// <summary>
    /// Handles logging configuration for the lite profile.
    /// </summary>
    public class LiteLoggingHandler : IConfigureLoggingForProfile<Lite>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            if (SetLoggingLibrary.Log4NetExists)
                SetLoggingLibrary.Log4Net(null, Logging.Loggers.Log4NetAdapter.AppenderFactory.CreateColoredConsoleAppender("Info"));
            else if (SetLoggingLibrary.NLogExists)
                SetLoggingLibrary.NLog(Logging.Loggers.NLogAdapter.TargetFactory.CreateColoredConsoleTarget());
            else
                throw new ConfigurationErrorsException("No logging framework found. NServiceBus supports log4net and NLog. You need to put any of these in the same directory as the host.");
        }
    }
}