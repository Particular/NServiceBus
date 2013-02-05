namespace NServiceBus.AcceptanceTesting.Support
{
    using NServiceBus.Config.ConfigurationSource;

    public interface IEndpointSetupTemplate
    {
        Configure GetConfiguration(RunDescriptor runDescriptor, EndpointBehavior endpointBehavior, IConfigurationSource configSource);
    }
}