namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using Support;

    public class EndpointConfigurationBuilder : IEndpointConfigurationFactory
    {
        public EndpointConfigurationBuilder()
        {
            configuration.EndpointMappings = new Dictionary<Type, Type>();
        }

        public EndpointConfigurationBuilder AuditTo<T>()
        {
            configuration.AuditEndpoint = typeof(T);
            return this;
        }

        public EndpointConfigurationBuilder AuditTo(Address addressOfAuditQueue)
        {
            configuration.AddressOfAuditQueue = addressOfAuditQueue;
            return this;
        }

        public EndpointConfigurationBuilder CustomMachineName(string customMachineName)
        {
            configuration.CustomMachineName = customMachineName;

            return this;
        }

        public EndpointConfigurationBuilder CustomEndpointName(string customEndpointName)
        {
            configuration.CustomEndpointName = customEndpointName;

            return this;
        }

        public EndpointConfigurationBuilder AddMapping<T>(Type endpoint)
        {
            configuration.EndpointMappings.Add(typeof(T),endpoint);

            return this;
        }

        EndpointConfiguration CreateScenario()
        {
            configuration.BuilderType = GetType();

            return configuration;
        }

        public EndpointConfigurationBuilder EndpointSetup<T>() where T : IEndpointSetupTemplate, new()
        {
            return EndpointSetup<T>(c => { });
        }

        public EndpointConfigurationBuilder EndpointSetup<T>(Action<Configure> configCustomization, Action<ConfigurationBuilder> configurationBuilderCustomization = null) where T : IEndpointSetupTemplate, new()
        {
            if (configurationBuilderCustomization == null)
            {
                configurationBuilderCustomization = b => { };
            }
            configuration.GetConfiguration = (settings,routingTable) =>
                {
                    var endpointSetupTemplate = new T();
                    var scenarioConfigSource = new ScenarioConfigSource(configuration, routingTable);
                    var config = endpointSetupTemplate.GetConfiguration(settings, configuration, scenarioConfigSource, configurationBuilderCustomization);
                    configCustomization(config);
                    return config;
                };

            return this;
        }

        EndpointConfiguration IEndpointConfigurationFactory.Get()
        {
            return CreateScenario();
        }

       
        readonly EndpointConfiguration configuration = new EndpointConfiguration();

        public EndpointConfigurationBuilder WithConfig<T>(Action<T> action) where T : new()
        {
            var config = new T();

            action(config);

            configuration.UserDefinedConfigSections[typeof (T)] = config;
            
            return this;
        }

        public EndpointConfigurationBuilder ExcludeType<T>()
        {
            configuration.TypesToExclude.Add(typeof(T));

            return this;
        }

        public EndpointConfigurationBuilder IncludeType<T>()
        {
            configuration.TypesToInclude.Add(typeof(T));

            return this;
        }
    }
}