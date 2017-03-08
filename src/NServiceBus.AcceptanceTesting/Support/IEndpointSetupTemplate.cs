// Disable obsolete warning until MessageEndpointMappings has been removed from config
#pragma warning disable CS0612, CS0619, CS0618
namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;
    using Config.ConfigurationSource;

    public interface IEndpointSetupTemplate
    {
        Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization);
    }
}
#pragma warning restore CS0612, CS0619, CS0618