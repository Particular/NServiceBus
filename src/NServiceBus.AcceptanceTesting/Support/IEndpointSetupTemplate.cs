namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;
    using Config.ConfigurationSource;

    public interface IEndpointSetupTemplate
    {
#pragma warning disable CS0618
        Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization);
#pragma warning restore CS0618
    }
}
