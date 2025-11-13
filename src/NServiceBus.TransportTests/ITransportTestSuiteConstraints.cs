namespace NServiceBus.TransportTests;

public interface ITransportTestSuiteConstraints
{
    IConfigureTransportInfrastructure CreateTransportConfiguration();
}

public partial class TransportTestSuiteConstraints : ITransportTestSuiteConstraints
{
    public static TransportTestSuiteConstraints Current { get; } = new TransportTestSuiteConstraints();
}