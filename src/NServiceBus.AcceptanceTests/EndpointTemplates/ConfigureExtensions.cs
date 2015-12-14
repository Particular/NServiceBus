namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using ScenarioDescriptors;

    public static class ConfigureExtensions
    {
        public static string GetOrNull(this IDictionary<string, string> dictionary, string key)
        {
            if (!dictionary.ContainsKey(key))
            {
                return null;
            }

            return dictionary[key];
        }

        public static Task DefineTransport(this BusConfiguration config, IDictionary<string, string> settings, Type endpointBuilderType)
        {
            if (!settings.ContainsKey("Transport"))
            {
                settings = Transports.Default.Settings;
            }

            return ConfigureTestExecution(TestDependencyType.Transport, config, settings);


        }

        public static Task DefinePersistence(this BusConfiguration config, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Persistence"))
            { 
                settings = Persistence.Default.Settings;
            }

            return ConfigureTestExecution(TestDependencyType.Persistence, config, settings);
        }

        enum TestDependencyType
        {
            Transport,
            Persistence
        }

        private static async Task ConfigureTestExecution(TestDependencyType type, BusConfiguration config, IDictionary<string,string> settings)
        {
            var dependencyTypeString = type.ToString();

            var dependencyType = Type.GetType(settings[dependencyTypeString]);

            var typeName = "Configure" + dependencyType.Name;

            var configurerType = Type.GetType(typeName, false);

            if (configurerType == null)
                throw new InvalidOperationException($"Acceptance Test project must include a non-namespaced class named '{typeName}' implementing {typeof(IConfigureTestExecution).Name}. See {typeof(ConfigureMsmqTransport).FullName} for an example.");

            var configurer = Activator.CreateInstance(configurerType) as IConfigureTestExecution;

            if (configurer == null)
                throw new InvalidOperationException($"{typeName} does not implement {typeof(IConfigureTestExecution).Name}.");

            await configurer.Configure(config, settings).ConfigureAwait(false);

            var configSettings = config.GetSettings();

            List<IConfigureTestExecution> cleaners;
            if (!configSettings.TryGet("Cleaners", out cleaners))
            {
                cleaners = new List<IConfigureTestExecution>();
                configSettings.Set("Cleaners", cleaners);
            }
            cleaners.Add(configurer);
        }

        class Cleaner
        {
            public Task Cleanup()
            {
                return Task.FromResult(0);
            }
        }

        public static void DefineBuilder(this BusConfiguration config, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Builder"))
            {
                var builderDescriptor = Builders.Default;

                if (builderDescriptor == null)
                {
                    return; //go with the default builder
                }

                settings = builderDescriptor.Settings;
            }

            var builderType = Type.GetType(settings["Builder"]);


            var typeName = "Configure" + builderType.Name;

            var configurerType = Type.GetType(typeName, false);

            if (configurerType != null)
            {
                var configurer = Activator.CreateInstance(configurerType);

                dynamic dc = configurer;

                dc.Configure(config);
            }

            config.UseContainer(builderType);
        }
    }
}