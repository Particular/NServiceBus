namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            return new ExternallyManagedContainerServer()
                .GetConfiguration(runDescriptor, endpointCustomizationConfiguration, endpointConfiguration =>
                {
                    endpointConfiguration.UseContainer(new AcceptanceTestingContainer());
                    configurationBuilderCustomization(endpointConfiguration);
                });
        }
    }
}
