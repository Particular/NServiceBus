namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus;

    public class DefaultServer : IEndpointSetupTemplate
    {

        public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource)
        {
            var settings = runDescriptor.Settings;
            SetupLogging(endpointConfiguration);

            var types = GetTypesToUse(endpointConfiguration);

            var transportToUse = settings.GetOrNull("Transport");

            var config = Configure.With(types)
                            .DefineEndpointName(endpointConfiguration.EndpointName)
                            .DefineBuilder(settings.GetOrNull("Builder"))
                            .CustomConfigurationSource(configSource)
                            .DefineSerializer(settings.GetOrNull("Serializer"))
                            .DefineTransport(transportToUse)
                            .Sagas()
                            .DefineSagaPersister(settings.GetOrNull("SagaPersister"));

            if (transportToUse == null || transportToUse.Contains("Msmq") || transportToUse.Contains("SqlServer") || transportToUse.Contains("RabbitMq"))
                config.UseInMemoryTimeoutPersister();

            if (transportToUse == null || transportToUse.Contains("Msmq") || transportToUse.Contains("SqlServer"))
                config.DefineSubscriptionStorage(settings.GetOrNull("SubscriptionStorage"));

            return config.UnicastBus();
        }

        static IEnumerable<Type> GetTypesToUse(EndpointConfiguration endpointConfiguration)
        {
            var assemblies = AssemblyScanner.GetScannableAssemblies();

            var types = assemblies.Assemblies
                                 .SelectMany(a => a.GetTypes())
                                 .Where(
                                     t =>
                                     t.Assembly != Assembly.GetExecutingAssembly() || //exlude all test types by default
                                     t.DeclaringType == endpointConfiguration.BuilderType.DeclaringType || //but include types on the test level
                                     t.DeclaringType == endpointConfiguration.BuilderType); //and the specific types for this endpoint
            return types;

        }

        static void SetupLogging(EndpointConfiguration endpointConfiguration)
        {
            var logDir = "..\\..\\logfiles\\";

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, endpointConfiguration.EndpointName + ".txt");

            if (File.Exists(logFile))
                File.Delete(logFile);

            var logLevel = "WARN";
            var logLevelOverride =  Environment.GetEnvironmentVariable("tests_loglevel");

            if (!string.IsNullOrEmpty(logLevelOverride))
                logLevel = logLevelOverride;

            SetLoggingLibrary.Log4Net(null,
                                      Logging.Loggers.Log4NetAdapter.Log4NetAppenderFactory.CreateRollingFileAppender(logLevel, logFile));
        }
    }
}