namespace NServiceBus.AcceptanceTesting.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting.Support;

    public class DefaultPublisher : IEndpointSetupTemplate
    {
        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
            new DefaultServer().GetConfiguration(runDescriptor, endpointConfiguration, configurationBuilderCustomization);
    }
}
