namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using ObjectBuilder;
    using ScenarioDescriptors;

    public static class ConfigureExtensions
    {
        public static Task DefineTransport(this EndpointConfiguration config, RunSettings settings, string endpointName)
        {
            Type transportType;
            if (!settings.TryGet("Transport", out transportType))
            {
                settings.Merge(Transports.Default.Settings);
            }

            return ConfigureTestExecution(TestDependencyType.Transport, config, settings, endpointName);
        }

        public static Task DefinePersistence(this EndpointConfiguration config, RunSettings settings, string endpointName)
        {
            Type persistenceType;
            if (!settings.TryGet("Persistence", out persistenceType))
            {
                settings.Merge(Persistence.Default.Settings);
            }

            return ConfigureTestExecution(TestDependencyType.Persistence, config, settings, endpointName);
        }

        public static void DefineBuilder(this EndpointConfiguration config, RunSettings settings)
        {
            Type builderType;
            if (!settings.TryGet("Builder", out builderType))
            {
                var builderDescriptor = Builders.Default;

                if (builderDescriptor == null)
                {
                    return; //go with the default builder
                }

                settings.Merge(builderDescriptor.Settings);
            }

            builderType = settings.Get<Type>("Builder");

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

        public static void RegisterComponentsAndInheritanceHierarchy(this EndpointConfiguration builder, RunDescriptor runDescriptor)
        {
            builder.RegisterComponents(r => { RegisterInheritanceHierarchyOfContextOnContainer(runDescriptor, r); });
        }

        static async Task ConfigureTestExecution(TestDependencyType type, EndpointConfiguration config, RunSettings settings, string endpointName)
        {
            var dependencyTypeString = type.ToString();

            var dependencyType = settings.Get<Type>(dependencyTypeString);

            var typeName = "ConfigureEndpoint" + dependencyType.Name;

            var configurerType = Type.GetType(typeName, false);

            if (configurerType == null)
            {
                throw new InvalidOperationException($"Acceptance Test project must include a non-namespaced class named '{typeName}' implementing {typeof(IConfigureEndpointTestExecution).Name}. See {typeof(ConfigureEndpointMsmqTransport).FullName} for an example.");
            }

            var configurer = Activator.CreateInstance(configurerType) as IConfigureEndpointTestExecution;

            if (configurer == null)
            {
                throw new InvalidOperationException($"{typeName} does not implement {typeof(IConfigureEndpointTestExecution).Name}.");
            }

            await configurer.Configure(endpointName, config, settings).ConfigureAwait(false);

            ActiveTestExecutionConfigurer cleaners;
            var cleanerKey = "ConfigureTestExecution." + endpointName;
            if (!settings.TryGet(cleanerKey, out cleaners))
            {
                cleaners = new ActiveTestExecutionConfigurer();
                settings.Set(cleanerKey, cleaners);
            }
            cleaners.Add(configurer);
        }

        static void RegisterInheritanceHierarchyOfContextOnContainer(RunDescriptor runDescriptor, IConfigureComponents r)
        {
            var type = runDescriptor.ScenarioContext.GetType();
            while (type != typeof(object))
            {
                r.RegisterSingleton(type, runDescriptor.ScenarioContext);
                type = type.BaseType;
            }
        }

        enum TestDependencyType
        {
            Transport,
            Persistence
        }
    }
}