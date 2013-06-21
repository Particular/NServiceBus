namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Hosting.Helpers;
    using NServiceBus;
    using Settings;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource)
        {
            var settings = runDescriptor.Settings;
            
            SetupLogging(endpointConfiguration);

            var types = GetTypesToUse(endpointConfiguration);

            var transportToUse = settings.GetOrNull("Transport");
            
            Configure.Features.Enable<Features.Sagas>();
            SettingsHolder.SetDefault("ScaleOut.UseSingleBrokerQueue", true);

            var config = Configure.With(types)
                            .DefineEndpointName(endpointConfiguration.EndpointName)
                            .DefineBuilder(settings.GetOrNull("Builder"))
                            .CustomConfigurationSource(configSource)
                            .DefineSerializer(settings.GetOrNull("Serializer"))
                            .DefineTransport(settings)
                            .DefineSagaPersister(settings.GetOrNull("SagaPersister"));

            if (transportToUse == null || 
                transportToUse.Contains("Msmq") || 
                transportToUse.Contains("SqlServer") || 
                transportToUse.Contains("RabbitMq") || 
                transportToUse.Contains("AzureServiceBus") ||
                transportToUse.Contains("AzureStorageQueue"))
                config.UseInMemoryTimeoutPersister();

            if (transportToUse == null || transportToUse.Contains("Msmq") || transportToUse.Contains("SqlServer"))
                config.DefineSubscriptionStorage(settings.GetOrNull("SubscriptionStorage"));

            return config.UnicastBus();
        }

        static IEnumerable<Type> GetTypesToUse(EndpointConfiguration endpointConfiguration)
        {
            var assemblies = AssemblyScanner.GetScannableAssemblies();

            var types = assemblies.Assemblies
                                    //exclude all test types by default
                                  .Where(a => a != Assembly.GetExecutingAssembly())
                                  .SelectMany(a => a.GetTypes());


            types = types.Union(GetNestedTypeRecursive(endpointConfiguration.BuilderType.DeclaringType));

            return types.Where(t=>!endpointConfiguration.TypesToExclude.Contains(t)).ToList();
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType)
        {
            yield return rootType;

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(GetNestedTypeRecursive))
            {
                yield return nestedType;
            }
        }

        static void SetupLogging(EndpointConfiguration endpointConfiguration)
        {
            var logDir = ".\\logfiles\\";

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