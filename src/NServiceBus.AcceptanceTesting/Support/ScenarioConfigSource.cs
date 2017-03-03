namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Configuration;
    using Config;
    using Config.ConfigurationSource;
    using Customization;

    public class ScenarioConfigSource : IConfigurationSource
    {
        EndpointCustomizationConfiguration configuration;

        public ScenarioConfigSource(EndpointCustomizationConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public T GetConfiguration<T>() where T : class, new()
        {
            var type = typeof(T);

            object configurationSection;
            if (configuration.UserDefinedConfigSections.TryGetValue(type, out configurationSection))
            {
                return configurationSection as T;
            }

            if (type == typeof(UnicastBusConfig))
            {

                return new UnicastBusConfig
                {
                    MessageEndpointMappings = GenerateMappings()
                } as T;

            }


            return ConfigurationManager.GetSection(type.Name) as T;
        }

        MessageEndpointMappingCollection GenerateMappings()
        {
            var mappings = new MessageEndpointMappingCollection();

            foreach (var templateMapping in configuration.EndpointMappings)
            {
                var messageType = templateMapping.Key;
                var endpoint = templateMapping.Value;

                mappings.Add(new MessageEndpointMapping
                     {
                         AssemblyName = messageType.Assembly.FullName,
                         TypeFullName = messageType.FullName,
                         Endpoint = Conventions.EndpointNamingConvention(endpoint)
                     });
            }

            return mappings;
        }
    }
}