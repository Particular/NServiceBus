namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    using Internal;
    using Logging.Loggers.Log4NetAdapter;
    using Logging.Loggers.NLogAdapter;

    /// <summary>
    /// Handles logging configuration for the lite profile.
    /// </summary>
    public class LiteLoggingHandler : IConfigureLoggingForProfile<Lite>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            if (Log4NetConfigurator.Log4NetExists)
                SetLoggingLibrary.Log4Net(null, Log4NetAppenderFactory.CreateColoredConsoleAppender("Info"));
            else if (NLogConfigurator.NLogExists)
                SetLoggingLibrary.NLog(null, NLogTargetFactory.CreateColoredConsoleTarget());
            else
                ConfigureInternalLog4Net.Lite();
        }
    }
}