namespace NServiceBus.IntegrationTests.Support
{
    using System.Collections.Generic;
    using Config.ConfigurationSource;

    public interface IEndpointSetupTemplate
    {
        Configure GetConfiguration(RunDescriptor runDescriptor, EndpointBehavior endpointBehavior, IConfigurationSource configSource);
    }
}