namespace NServiceBus.IntegrationTests.Automated.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Config.ConfigurationSource;
    using Hosting.Helpers;
    using NServiceBus;
    using Support;

    public class DefaultServer : IEndpointSetupTemplate
    {

        public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointBehavior endpointBehavior, IConfigurationSource configSource)
        {
            var settings = runDescriptor.Settings;
            SetupLogging(endpointBehavior);

            var types = GetTypesToUse(endpointBehavior);

            return Configure.With(types)
                            .DefineEndpointName(endpointBehavior.EndpointName)
                            .DefineBuilder(settings.GetOrNull("Builder"))
                            .CustomConfigurationSource(configSource)
                            .DefineSerializer(settings.GetOrNull("Serializer"))
                            .DefineTransport(settings.GetOrNull("Transport"))
                            .UseInMemoryTimeoutPersister()
                            .UnicastBus();
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