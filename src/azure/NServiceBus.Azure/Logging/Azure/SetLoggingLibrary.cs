using NServiceBus.Logging;
using NServiceBus.Logging.Loggers;

namespace NServiceBus
{
    using Integration.Azure;

    public static class SetLoggingLibraryForAzure
    {
        public static Configure ConsoleLogger(this Configure config)
        {
            LogManager.LoggerFactory = new ConsoleLoggerFactory();
            return config;
        }

        public static Configure AzureDiagnosticsLogger(this Configure config, bool enable = true, bool initialize = true)
        {
            var factory = new AzureDiagnosticsLoggerFactory {Enable = enable, InitializeDiagnostics = initialize};
            factory.ConfigureAzureDiagnostics();
            LogManager.LoggerFactory = factory;
            return config;
        }
    }
}