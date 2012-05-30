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
        }
    }
}