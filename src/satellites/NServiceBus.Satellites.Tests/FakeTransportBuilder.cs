namespace NServiceBus.Satellites.Tests
{
    using Unicast.Transport;

    public class FakeTransportBuilder : ISatelliteTransportBuilder
    {
        public ITransport TransportToReturn { get; set; }

        public ITransport Build()
        {
            return TransportToReturn ?? new FakeTransport();
        }
    }
}