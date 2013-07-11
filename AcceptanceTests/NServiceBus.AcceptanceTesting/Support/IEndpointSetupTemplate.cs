namespace NServiceBus.AcceptanceTesting.Support
{
    using NServiceBus.Config.ConfigurationSource;

    public interface IEndpointSetupTemplate
    {
        Configure GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource);
    }
}