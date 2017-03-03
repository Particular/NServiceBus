// Disable obsolete warning until MessageEndpointMappings has been removed from config
#pragma warning disable CS0612, CS0619, CS0618
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

            return ConfigurationManager.GetSection(type.Name) as T;
        }
    }
}
#pragma warning restore CS0612, CS0619, CS0618