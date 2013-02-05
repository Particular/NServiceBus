namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Hosting.Helpers;
    using NServiceBus;

    public class DefaultServer : IEndpointSetupTemplate
    {

        public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointBehavior endpointBehavior, IConfigurationSource configSource)
        {
            var settings = runDescriptor.Settings;
            SetupLogging(endpointBehavior);

            var types = GetTypesToUse(endpointBehavior);

            var transportToUse = settings.GetOrNull("Transport");

            var config = Configure.With(types)
                            .DefineEndpointName(endpointBehavior.EndpointName)
                            .DefineBuilder(settings.GetOrNull("Builder"))
                            .CustomConfigurationSource(configSource)
                            .DefineSerializer(settings.GetOrNull("Serializer"))
                            .DefineTransport(transportToUse);

            if (transportToUse == null || transportToUse.Contains("Msmq") || transportToUse.Contains("SqlServer") || transportToUse.Contains("RabbitMq"))
                config.UseInMemoryTimeoutPersister();

            if (transportToUse == null || transportToUse.Contains("Msmq") || transportToUse.Contains("SqlServer"))
                config.InMemorySubscriptionStorage();

            return config.UnicastBus();
        }

        static IEnumerable<Type> GetTypesToUse(EndpointBehavior endpointBehavior)
        {
            var assemblies = AssemblyScanner.GetScannableAssemblies();

            var types = assemblies.Assemblies
                                 .SelectMany(a => a.GetTypes())
                                 .Where(
                                     t =>
                                     t.Assembly != Assembly.GetExecutingAssembly() || //exlude all test types by default
                                     t.DeclaringType == endpointBehavior.BuilderType.DeclaringType || //but include types on the test level
                                     t.DeclaringType == endpointBehavior.BuilderType); //and the specific types for this endpoint
            return types;

        }

        static void SetupLogging(EndpointBehavior endpointBehavior)
        {
            var logDir = "..\\..\\logfiles\\";

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, endpointBehavior.EndpointName + ".txt");

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