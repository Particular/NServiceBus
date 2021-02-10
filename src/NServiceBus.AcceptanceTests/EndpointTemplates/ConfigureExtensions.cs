namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using Configuration.AdvancedExtensibility;
    using Transport;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting.Support;
    using Microsoft.Extensions.DependencyInjection;

    public static class ConfigureExtensions
    {
        public static RoutingSettings ConfigureRouting(this EndpointConfiguration configuration) =>
            new RoutingSettings(configuration.GetSettings());

        // This is kind of a hack because the acceptance testing framework doesn't give any access to the transport definition to individual tests.
        public static TransportDefinition ConfigureTransport(this EndpointConfiguration configuration) =>
            configuration.GetSettings().Get<TransportDefinition>();

        public static TTransportDefinition ConfigureTransport<TTransportDefinition>(
            this EndpointConfiguration configuration)
            where TTransportDefinition : TransportDefinition =>
            (TTransportDefinition)configuration.GetSettings().Get<TransportDefinition>();

        public static async Task DefineTransport(this EndpointConfiguration config, RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration)
        {
            var transportConfiguration = TestSuiteConstraints.Current.CreateTransportConfiguration();
            await transportConfiguration.Configure(endpointCustomizationConfiguration.EndpointName, config, runDescriptor.Settings, endpointCustomizationConfiguration.PublisherMetadata);
            runDescriptor.OnTestCompleted(_ => transportConfiguration.Cleanup());
        }

        public static async Task DefineTransport(this EndpointConfiguration config, IConfigureEndpointTestExecution transportConfiguration, RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration)
        {
            await transportConfiguration.Configure(endpointCustomizationConfiguration.EndpointName, config, runDescriptor.Settings, endpointCustomizationConfiguration.PublisherMetadata);
            runDescriptor.OnTestCompleted(_ => transportConfiguration.Cleanup());
        }

        public static async Task DefinePersistence(this EndpointConfiguration config, RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration)
        {
            var persistenceConfiguration = TestSuiteConstraints.Current.CreatePersistenceConfiguration();
            await persistenceConfiguration.Configure(endpointCustomizationConfiguration.EndpointName, config, runDescriptor.Settings, endpointCustomizationConfiguration.PublisherMetadata);
            runDescriptor.OnTestCompleted(_ => persistenceConfiguration.Cleanup());
        }

        public static async Task DefinePersistence(this EndpointConfiguration config, IConfigureEndpointTestExecution persistenceConfiguration, RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration)
        {
            await persistenceConfiguration.Configure(endpointCustomizationConfiguration.EndpointName, config, runDescriptor.Settings, endpointCustomizationConfiguration.PublisherMetadata);
            runDescriptor.OnTestCompleted(_ => persistenceConfiguration.Cleanup());
        }

        public static void RegisterComponentsAndInheritanceHierarchy(this EndpointConfiguration builder, RunDescriptor runDescriptor)
        {
            builder.RegisterComponents(r => { RegisterInheritanceHierarchyOfContextOnContainer(runDescriptor, r); });
        }

        static void RegisterInheritanceHierarchyOfContextOnContainer(RunDescriptor runDescriptor, IServiceCollection r)
        {
            var type = runDescriptor.ScenarioContext.GetType();
            while (type != typeof(object))
            {
                r.AddSingleton(type, runDescriptor.ScenarioContext);
                type = type.BaseType;
            }
        }
    }
}