namespace NServiceBus.TransportTests;

public partial class TransportTestSuiteConstraints : ITransportTestSuiteConstraints
{
    public IConfigureTransportInfrastructure CreateTransportConfiguration() => new ConfigureLearningTransportInfrastructure();
}