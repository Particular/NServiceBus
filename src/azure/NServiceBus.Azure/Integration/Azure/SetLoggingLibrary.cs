using NServiceBus.Logging;
using NServiceBus.Logging.Loggers;

namespace NServiceBus.Integration.Azure
{
    public static class SetLoggingLibrary
    {
        public static void ConsoleLogger(this Configure config)
        {
            LogManager.LoggerFactory = new ConsoleLoggerFactory();
        }

        public static void AzureDiagnosticsLogger(this Configure config, bool enable, bool initialize = true)
        {
            var factory = new AzureDiagnosticsLoggerFactory {Enable = enable, InitializeDiagnostics = initialize};
            factory.ConfigureAzureDiagnostics();
            LogManager.LoggerFactory = factory;
        }
    }
}