namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using Config.ConfigurationSource;

    public interface IEndpointSetupTemplate
    {
        BusConfiguration GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource, Action<BusConfiguration> configurationBuilderCustomization);
    }
}