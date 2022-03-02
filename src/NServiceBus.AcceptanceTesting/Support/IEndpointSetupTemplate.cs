namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;

    public interface IEndpointSetupTemplate
    {
        Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization);
    }
}
