namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using NServiceBus.Config.ConfigurationSource;

    public class DefaultPublisher : IEndpointSetupTemplate
    {
// Disable obsolete warning until MessageEndpointMappings has been removed from config and we can remove the parameter completetely
#pragma warning disable CS0612, CS0619, CS0618
        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            return new DefaultServer().GetConfiguration(runDescriptor, endpointConfiguration, configSource, configurationBuilderCustomization);
        }
#pragma warning restore CS0612, CS0619, CS0618
    }
}