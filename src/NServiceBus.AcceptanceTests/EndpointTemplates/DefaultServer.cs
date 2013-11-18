namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Hosting.Helpers;
    using log4net.Appender;
    using log4net.Core;
    using NServiceBus;
    using Settings;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public Configure GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource)
        {
            var settings = runDescriptor.Settings;

            SetupLogging(endpointConfiguration, runDescriptor.ScenarioContext);

            var types = GetTypesToUse(endpointConfiguration);

            var transportToUse = settings.GetOrNull("Transport");

            Configure.Features.Enable<Features.Sagas>();

            SettingsHolder.SetDefault("ScaleOut.UseSingleBrokerQueue", true);

            var config = Configure.With(types)
                            .DefineEndpointName(endpointConfiguration.EndpointName)
                            .CustomConfigurationSource(configSource)
                            .DefineBuilder(settings.GetOrNull("Builder"))
                            .DefineSerializer(settings.GetOrNull("Serializer"))
                            .DefineTransport(settings)
                            .DefineSagaPersister(settings.GetOrNull("SagaPersister"));

            if (transportToUse == null ||
                transportToUse.Contains("Msmq") ||
                transportToUse.Contains("SqlServer") ||
                transportToUse.Contains("RabbitMq"))
                config.UseInMemoryTimeoutPersister();

            if (transportToUse == null || transportToUse.Contains("Msmq") || transportToUse.Contains("SqlServer"))
                config.DefineSubscriptionStorage(settings.GetOrNull("SubscriptionStorage"));

            return config.UnicastBus();
        }

        static IEnumerable<Type> GetTypesToUse(EndpointConfiguration endpointConfiguration)
        {
            var assemblies = new AssemblyScanner().GetScannableAssemblies();

            var types = assemblies.Assemblies
                //exclude all test types by default
                                  .Where(a => a != Assembly.GetExecutingAssembly())
                                  .SelectMany(a => a.GetTypes());


            types = types.Union(GetNestedTypeRecursive(endpointConfiguration.BuilderType.DeclaringType, endpointConfiguration.BuilderType));

            types = types.Union(endpointConfiguration.TypesToInclude);

            return types.Where(t => !endpointConfiguration.TypesToExclude.Contains(t)).ToList();
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
                yield break;

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }

        static void SetupLogging(EndpointConfiguration endpointConfiguration, ScenarioContext scenarioContext)
        {
            var logDir = ".\\logfiles\\";

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, endpointConfiguration.EndpointName + ".txt");

            if (File.Exists(logFile))
                File.Delete(logFile);

            var logLevel = "WARN";
            var logLevelOverride = Environment.GetEnvironmentVariable("tests_loglevel");

            if (!string.IsNullOrEmpty(logLevelOverride))
                logLevel = logLevelOverride;

            try
            {
                SetLoggingLibrary.Log4Net(null, new ContextAppender(scenarioContext));
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex);

            }

        }
    }

    public class ContextAppender : AppenderSkeleton
    {
        public ContextAppender(ScenarioContext context)
        {
            this.context = context;
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent.ExceptionObject != null)
            {
                lock (context)
                {
                    context.Exceptions += loggingEvent.ExceptionObject + "/n/r";
                }
            }

        }

        ScenarioContext context;
    }
}