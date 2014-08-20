namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using Config.ConfigurationSource;

    public interface IEndpointSetupTemplate
    {
        ConfigurationBuilder GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource, Action<ConfigurationBuilder> configurationBuilderCustomization);
    }
}