namespace NServiceBus.IntegrationTests.Automated.EndpointTemplates
{
    using System.IO;
    using Config.ConfigurationSource;
    using NServiceBus;
    using Support;

    public class DefaultServer : IEndpointSetupTemplate
    {

        public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointBehavior endpointBehavior, IConfigurationSource configSource)
        {
            var settings = runDescriptor.Settings;
            SetupLogging(endpointBehavior);

            return Configure.With()
                    .DefineEndpointName(endpointBehavior.EndpointName)
                    .Log4Net()
                    .DefineBuilder(settings.GetOrNull("Builder"))
                    .CustomConfigurationSource(configSource)
                    .DefineSerializer(settings.GetOrNull("Serializer"))
                    .DefineTransport(settings.GetOrNull("Transport"))
                    .UnicastBus();

        }

        static void SetupLogging(EndpointBehavior endpointBehavior)
        {
            var logDir = "..\\..\\logfiles\\";

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, endpointBehavior.EndpointName + ".txt");

            if (File.Exists(logFile))
                File.Delete(logFile);

            SetLoggingLibrary.Log4Net(null,
                                      Logging.Loggers.Log4NetAdapter.Log4NetAppenderFactory.CreateRollingFileAppender(null, logFile));
        }
    }
}