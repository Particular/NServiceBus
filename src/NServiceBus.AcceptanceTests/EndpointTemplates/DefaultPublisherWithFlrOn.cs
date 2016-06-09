namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using NServiceBus.Config.ConfigurationSource;

    public class DefaultPublisherWithFlrOn : IEndpointSetupTemplate
    {
        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            return new DefaultServerWithFlrOn().GetConfiguration(runDescriptor, endpointConfiguration, configSource, configurationBuilderCustomization);
        }
    }
}