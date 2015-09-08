namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;
    using Config.ConfigurationSource;

    public interface IEndpointSetupTemplate
    {
        Task<BusConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource, Action<BusConfiguration> configurationBuilderCustomization);
    }
}