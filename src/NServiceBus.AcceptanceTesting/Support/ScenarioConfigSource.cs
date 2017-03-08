#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Configuration;
    using Config.ConfigurationSource;

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

            return ConfigurationManager.GetSection(type.Name) as T;
        }
    }
}
#pragma warning restore CS0618