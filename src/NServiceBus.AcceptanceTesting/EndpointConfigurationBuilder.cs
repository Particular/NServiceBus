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

        public EndpointConfigurationBuilder AuditTo(string addressOfAuditQueue)
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
            configuration.EndpointMappings.Add(typeof(T), endpoint);

            return this;
        }
        public EndpointConfigurationBuilder PerformDefaultRetries()
        {
            configuration.PerformDefaultRetries = true;

            return this;
        }

        EndpointCustomizationConfiguration CreateScenario()
        {
            configuration.BuilderType = GetType();

            return configuration;
        }

        public EndpointConfigurationBuilder EndpointSetup<T>() where T : IEndpointSetupTemplate, new()
        {
            return EndpointSetup<T>(c => { });
        }

        public EndpointConfigurationBuilder EndpointSetup<T>(Action<EndpointConfiguration> configurationBuilderCustomization) where T : IEndpointSetupTemplate, new()
        {
            if (configurationBuilderCustomization == null)
            {
                configurationBuilderCustomization = b => { };
            }

            return EndpointSetup<T>((bc, esc) =>
            {
                configurationBuilderCustomization(bc);
            });
        }

        public EndpointConfigurationBuilder EndpointSetup<T>(Action<EndpointConfiguration, RunDescriptor> configurationBuilderCustomization) where T : IEndpointSetupTemplate, new()
        {
            if (configurationBuilderCustomization == null)
            {
                configurationBuilderCustomization = (rd, b) => { };
            }
            configuration.GetConfiguration = async (runDescriptor, routingTable) =>
            {
                var endpointSetupTemplate = new T();
                var scenarioConfigSource = new ScenarioConfigSource(configuration, routingTable);
                var endpointConfiguration = await endpointSetupTemplate.GetConfiguration(runDescriptor, configuration, scenarioConfigSource, bc =>
                {
                    configurationBuilderCustomization(bc, runDescriptor);
                }).ConfigureAwait(false);

                return endpointConfiguration;
            };

            return this;
        }


        EndpointCustomizationConfiguration IEndpointConfigurationFactory.Get()
        {
            return CreateScenario();
        }
        public ScenarioContext ScenarioContext { get; set; }


        EndpointCustomizationConfiguration configuration = new EndpointCustomizationConfiguration();

        public EndpointConfigurationBuilder WithConfig<T>(Action<T> action) where T : new()
        {
            var config = new T();

            action(config);

            configuration.UserDefinedConfigSections[typeof(T)] = config;

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

        public EndpointConfigurationBuilder SendOnly()
        {
            configuration.SendOnly = true;

            return this;
        }
    }
}