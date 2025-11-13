namespace NServiceBus.TransportTests;

public partial class TransportTestsConfiguration : ITransportTestsConfiguration
{
    public IConfigureTransportInfrastructure CreateTransportConfiguration() => new ConfigureLearningTransportInfrastructure();
}