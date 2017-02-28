namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using Support;
    using System.Configuration;

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

        EndpointCustomizationConfiguration CreateScenario()
        {
            configuration.BuilderType = GetType();

            return configuration;
        }

        public EndpointConfigurationBuilder EndpointSetup<T>(Action<EndpointConfiguration> configurationBuilderCustomization = null,
            Action<PublisherMetadata> publisherMetadata = null) where T : IEndpointSetupTemplate, new()
        {
            if (configurationBuilderCustomization == null)
            {
                configurationBuilderCustomization = b => { };
            }

            publisherMetadata?.Invoke(configuration.PublisherMetadata);

            return EndpointSetup<T>((bc, esc) =>
            {
                configurationBuilderCustomization(bc);
            });
        }

        public EndpointConfigurationBuilder EndpointSetup<T>(Action<EndpointConfiguration, RunDescriptor> configurationBuilderCustomization, Action<PublisherMetadata> publisherMetadata = null) where T : IEndpointSetupTemplate, new()
        {
            if (configurationBuilderCustomization == null)
            {
                configurationBuilderCustomization = (rd, b) => { };
            }

            publisherMetadata?.Invoke(configuration.PublisherMetadata);

            configuration.GetConfiguration = async (runDescriptor, routingTable) =>
            {
                var endpointSetupTemplate = new T();
                var scenarioConfigSource = new ScenarioConfigSource(configuration, routingTable);
                var endpointConfiguration = await endpointSetupTemplate.GetConfiguration(runDescriptor, configuration, scenarioConfigSource, bc =>
                {
                    configurationBuilderCustomization(bc, runDescriptor);
                }).ConfigureAwait(false);

                if (!configuration.SendOnly)
                {
                    if (configuration.AddressOfAuditQueue != null)
                    {
                        endpointConfiguration.AuditProcessedMessagesTo(configuration.AddressOfAuditQueue);
                    }
                    else if (configuration.AuditEndpoint != null)
                    {
                        if (!routingTable.ContainsKey(configuration.AuditEndpoint))
                        {
                            throw new ConfigurationErrorsException($"{configuration.AuditEndpoint} was not found in routingTable. Ensure that WithEndpoint<{configuration.AuditEndpoint}>() method is called in the test.");
                        }

                        endpointConfiguration.AuditProcessedMessagesTo(routingTable[configuration.AuditEndpoint]);
                    }
                }

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