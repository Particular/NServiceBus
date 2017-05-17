namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;
    using Config.ConfigurationSource;

    public interface IEndpointSetupTemplate
    {
        Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization);
    }
}
