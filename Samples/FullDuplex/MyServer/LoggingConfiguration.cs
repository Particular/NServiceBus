using NServiceBus;

namespace MyServer
{
    public class LoggingConfiguration : IConfigureLoggingForProfile<Production>
    {
        public void Configure(IConfigureThisEndpoint specifier)
        {
            SetLoggingLibrary.NLog();

            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(new NLog.Targets.ColoredConsoleTarget());
        }
    }
}