﻿namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using ObjectBuilder;

    public static class ConfigureExtensions
    {
        public static Task DefineTransport(this EndpointConfiguration config, RunSettings settings, EndpointCustomizationConfiguration endpointCustomizationConfiguration)
        {
            return ConfigureTestExecution(TestSuiteConstraints.Current.TransportConfiguration, config, settings, endpointCustomizationConfiguration.EndpointName, endpointCustomizationConfiguration.PublisherMetadata);
        }

        public static Task DefinePersistence(this EndpointConfiguration config, RunSettings settings, EndpointCustomizationConfiguration endpointCustomizationConfiguration)
        {
            return ConfigureTestExecution(TestSuiteConstraints.Current.PersistenceConfiguration, config, settings, endpointCustomizationConfiguration.EndpointName, endpointCustomizationConfiguration.PublisherMetadata);
        }

        public static void RegisterComponentsAndInheritanceHierarchy(this EndpointConfiguration builder, RunDescriptor runDescriptor)
        {
            builder.RegisterComponents(r => { RegisterInheritanceHierarchyOfContextOnContainer(runDescriptor, r); });
        }

        static async Task ConfigureTestExecution(IConfigureEndpointTestExecution configurer, EndpointConfiguration config, RunSettings settings, string endpointName, PublisherMetadata publisherMetadata)
        {
            if (configurer == null)
            {
                //todo review text
                //throw new InvalidOperationException($"Acceptance Test project must include a non-namespaced class named '{typeName}' implementing {typeof(IConfigureEndpointTestExecution).Name}. See {typeof(ConfigureEndpointMsmqTransport).FullName} for an example.");
            }

            await configurer.Configure(endpointName, config, settings, publisherMetadata).ConfigureAwait(false);

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