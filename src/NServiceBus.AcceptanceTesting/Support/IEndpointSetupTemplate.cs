#pragma warning disable CS0618
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
#pragma warning restore CS0618