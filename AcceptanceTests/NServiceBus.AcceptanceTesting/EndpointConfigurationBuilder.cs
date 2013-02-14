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

        public EndpointConfigurationBuilder AuditTo(Address addressOfAuditQueue)
        {
            configuration.AddressOfAuditQueue = addressOfAuditQueue;

            return this;
        }


        public EndpointConfigurationBuilder AppConfig(string path)
        {
            configuration.AppConfigPath = path;

            return this;
        }


        public EndpointConfigurationBuilder AddMapping<T>(Type endpoint)
        {
            this.configuration.EndpointMappings.Add(typeof(T),endpoint);

            return this;
        }

        EndpointConfiguration CreateScenario()
        {
            configuration.BuilderType = GetType();

            return this.configuration;
        }


    
        public EndpointConfigurationBuilder EndpointSetup<T>() where T : IEndpointSetupTemplate
        {
            return EndpointSetup<T>(c => { });
        }

        public EndpointConfigurationBuilder EndpointSetup<T>(Action<Configure> configCustomization) where T: IEndpointSetupTemplate
        {
            configuration.GetConfiguration = (settings,routingTable) =>
                {
                    var config = ((IEndpointSetupTemplate)Activator.CreateInstance<T>()).GetConfiguration(settings, configuration, new ScenarioConfigSource(configuration, routingTable));

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
    }
}